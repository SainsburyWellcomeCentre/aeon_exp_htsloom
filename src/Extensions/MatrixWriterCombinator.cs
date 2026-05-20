using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;
using Bonsai.Dsp;
using Bonsai.IO;
using OpenCV.Net;

namespace HtsLoom.Extensions
{
    // OPTION 2 — plain [Combinator] class that composes Bonsai.Dsp.MatrixWriter instead of inheriting.
    //
    // Pros: only Tuple-shaped Process overloads are exposed; there is no inherited non-tuple Process
    //       that could swallow a mis-wired connection.
    // Cons: every MatrixWriter / StreamSink property has to be re-declared here with description
    //       attributes mirrored by hand — additions to MatrixWriter in future Bonsai.Dsp releases
    //       will not propagate automatically. Migrating an existing dsp:MatrixWriter node in a
    //       .bonsai workflow also requires renaming each <dsp:X> property element to <ext:X>;
    //       Option 1 keeps those under <dsp:> unchanged.
    [Combinator]
    [Description(
        "Writes each array-like object in the sequence to a raw binary output stream. The path is " +
        "taken from Item2 of the input Tuple<T, string>, so each subscription opens its own stream " +
        "even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class MatrixWriterCombinator
    {
        public MatrixWriterCombinator()
        {
            Layout = MatrixLayout.ColumnMajor;
        }

        /// <summary>
        /// Gets or sets a value specifying the sequential memory layout used to store the sample buffers.
        /// </summary>
        [Description("Specifies the sequential memory layout used to store the sample buffers.")]
        public MatrixLayout Layout { get; set; }

        /// <summary>
        /// Gets or sets the suffix that should be applied to the path before creating the writer.
        /// </summary>
        [Description("The suffix that should be applied to the path before creating the writer.")]
        public PathSuffix Suffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output path if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output path if it already exists.")]
        public bool Overwrite { get; set; }

        public IObservable<byte[]> Process(IObservable<Tuple<byte[], string>> source)
        {
            return ProcessNamed(source, (inner, data) => inner.Process(data));
        }

        public IObservable<Mat> Process(IObservable<Tuple<Mat, string>> source)
        {
            return ProcessNamed(source, (inner, data) => inner.Process(data));
        }

        IObservable<T> ProcessNamed<T>(
            IObservable<Tuple<T, string>> source,
            Func<Bonsai.Dsp.MatrixWriter, IObservable<T>, IObservable<T>> writeFn)
        {
            return source.Publish(shared =>
                shared.Take(1).SelectMany(value =>
                {
                    var inner = new Bonsai.Dsp.MatrixWriter
                    {
                        Path      = value.Item2,
                        Layout    = Layout,
                        Suffix    = Suffix,
                        Overwrite = Overwrite,
                    };

                    var data = shared.Select(pair => pair.Item1).StartWith(value.Item1);
                    return writeFn(inner, data);
                }));
        }
    }
}

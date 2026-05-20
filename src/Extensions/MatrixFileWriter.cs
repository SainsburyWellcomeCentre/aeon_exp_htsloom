using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Xml.Serialization;
using Bonsai;
using Bonsai.Dsp;
using OpenCV.Net;

namespace HtsLoom.Extensions
{
    // OPTION 1 — inherits from Bonsai.Dsp.MatrixWriter.
    //
    // Pros: gets every MatrixWriter / StreamSink property (Layout, Path, Suffix, Overwrite) for
    //       free; inherited <dsp:...> property elements in the .bonsai XML stay unchanged when
    //       migrating from dsp:MatrixWriter. MatrixWriter exposes public Process overloads for
    //       Mat / byte[] / TElement[] / TElement, so no expression-builder reflection is needed —
    //       we just add Tuple-shaped overloads beside them and Bonsai picks the right one by input.
    // Cons: the inherited Process(IObservable<...>) overloads still exist; if you accidentally
    //       wire a non-Tuple stream into this node Bonsai will use the inherited overload and
    //       fall back to the static Path property.
    [Description(
        "Writes each array-like object in the sequence to a raw binary output stream. The path is " +
        "taken from Item2 of the input Tuple<T, string>, so each subscription opens its own stream " +
        "even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]

    public class MatrixFileWriter : Bonsai.Dsp.MatrixWriter
    {
        // Inherited Path is unused at runtime (the path comes from the input tuple). Hide it from
        // the property grid and the .bonsai XML so the editor doesn't show a confusing
        // always-overridden field and the saved workflow doesn't carry a stale path.
        [Browsable(false)]
        [XmlIgnore]
        public new string Path
        {
            get { return base.Path; }
            set { base.Path = value; }
        }

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

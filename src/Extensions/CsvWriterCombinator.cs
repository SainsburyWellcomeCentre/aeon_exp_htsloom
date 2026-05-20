using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using Bonsai;
using Bonsai.IO;

namespace HtsLoom.Extensions
{
    // OPTION 2 — plain [Combinator] class that composes Bonsai.IO.CsvWriter instead of inheriting from it.
    //
    // Pros: Bonsai auto-discovers Process<T> via the [Combinator] attribute, so the BuildCombinator
    //       override and the ProcessNamedMethod dispatch reflection from Option 1 disappear. The
    //       Tuple<T, string> shape is enforced by the method signature, so the runtime type-shape
    //       check from Option 1 is no longer needed either. The public surface is one method.
    // Cons: every CsvWriter property has to be re-declared here with its description and editor
    //       attributes mirrored by hand — additions to CsvWriter in future Bonsai.System releases
    //       will not propagate automatically. The reflection into CsvWriter.BuildCombinator stays
    //       because the inner CsvWriter has no public Process<T>(IObservable<T>) entry point — its
    //       row formatter only runs through the expression-builder pipeline. Migrating an existing
    //       io:CsvWriter node in a .bonsai workflow also requires renaming each <io:X> property
    //       element to <ext:X>; Option 1 keeps those under <io:> unchanged.
    [Combinator]
    [Description(
        "Writes a delimited text representation of each element of the sequence to a text file. " +
        "The file name is taken from Item2 of the input Tuple<T, string>, so each subscription " +
        "opens its own file even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class CsvWriterCombinator
    {
        static readonly MethodInfo InnerBuildCombinator = typeof(Bonsai.IO.CsvWriter).GetMethod(
            "BuildCombinator",
            BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Gets or sets the optional delimiter used to separate columns in the output file.
        /// </summary>
        [DefaultValue("")]
        [Description("The optional delimiter used to separate columns in the output file.")]
        public string Delimiter { get; set; }

        /// <summary>
        /// Gets or sets the separator used to delimit elements in variable length rows. This argument is optional.
        /// </summary>
        [Description("The separator used to delimit elements in variable length rows. This argument is optional.")]
        public string ListSeparator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether data should be appended to the output file if it already exists.
        /// </summary>
        [Description("Indicates whether data should be appended to the output file if it already exists.")]
        public bool Append { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output file should be overwritten if it already exists.
        /// </summary>
        [Description("Indicates whether the output file should be overwritten if it already exists.")]
        public bool Overwrite { get; set; }

        /// <summary>
        /// Gets or sets the suffix used to generate file names.
        /// </summary>
        [Description("The suffix used to generate file names.")]
        public PathSuffix Suffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include a text header with column names for multi-attribute values.
        /// </summary>
        [Description("Indicates whether to include a text header with column names for multi-attribute values.")]
        public bool IncludeHeader { get; set; }

        /// <summary>
        /// Gets or sets the inner properties that will be selected when writing each element of the sequence.
        /// </summary>
        [Description("The inner properties that will be selected when writing each element of the sequence.")]
        [Editor("Bonsai.IO.Design.DataMemberSelectorEditor, Bonsai.System.Design", DesignTypes.UITypeEditor)]
        public string Selector { get; set; }

        public IObservable<T> Process<T>(IObservable<Tuple<T, string>> source)
        {
            return source.Publish(shared =>
                shared.Take(1).SelectMany(value =>
                {
                    var inner = new Bonsai.IO.CsvWriter
                    {
                        FileName      = value.Item2,
                        Delimiter     = Delimiter,
                        ListSeparator = ListSeparator,
                        Append        = Append,
                        Overwrite     = Overwrite,
                        Suffix        = Suffix,
                        IncludeHeader = IncludeHeader,
                        Selector      = Selector,
                    };

                    var sourceParam = Expression.Parameter(typeof(IObservable<T>), "source");
                    var built = (Expression)InnerBuildCombinator.Invoke(
                        inner, new object[] { new[] { (Expression)sourceParam } });
                    var apply = Expression.Lambda<Func<IObservable<T>, IObservable<T>>>(built, sourceParam).Compile();

                    var rows = shared.Select(pair => pair.Item1).StartWith(value.Item1);
                    return apply(rows);
                }));
        }
    }
}

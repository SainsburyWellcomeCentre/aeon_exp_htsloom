using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Bonsai;

namespace HtsLoom.Extensions
{
    // OPTION 1 — inherits from Bonsai.IO.CsvWriter, overrides BuildCombinator.
    //
    // Pros: gets every CsvWriter property (including their editors) for free; the .bonsai XML keeps
    //       inherited <io:...> property elements unchanged when migrating from io:CsvWriter.
    // Cons: because Bonsai.IO.CsvWriter is a CombinatorExpressionBuilder, Bonsai will not auto-discover
    //       a Process method on the subclass — the only way to customise behaviour is to override the
    //       protected BuildCombinator. That forces the reflection call below.
    [Description(
        "Writes a delimited text representation of each element of the sequence to a text file. " +
        "The file name is taken from Item2 of the input Tuple<T, string>, so each subscription " +
        "opens its own file even when fanned out via SelectMany across parallel branches.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class CsvFileWriter : Bonsai.IO.CsvWriter
    {
        // Inherited FileName is unused at runtime (the path comes from the input tuple). Hide it
        // from the property grid and the .bonsai XML so the editor doesn't show a confusing
        // always-overridden field and the saved workflow doesn't carry a stale path.
        [Browsable(false)]
        [XmlIgnore]
        public new string FileName
        {
            get { return base.FileName; }
            set { base.FileName = value; }
        }

        static readonly MethodInfo InnerBuildCombinator = typeof(Bonsai.IO.CsvWriter).GetMethod(
            "BuildCombinator",
            BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly MethodInfo ProcessNamedMethod = typeof(CsvFileWriter).GetMethod(
            "ProcessNamed",
            BindingFlags.Instance | BindingFlags.NonPublic);

        protected override Expression BuildCombinator(IEnumerable<Expression> arguments)
        {
            var source = arguments.First();
            var elementType = source.Type.GetGenericArguments()[0];

            if (!elementType.IsGenericType ||
                elementType.GetGenericTypeDefinition() != typeof(Tuple<,>) ||
                elementType.GetGenericArguments()[1] != typeof(string))
            {
                throw new InvalidOperationException(
                    "CsvWriter requires an input of type IObservable<Tuple<T, string>>, " +
                    "where Item1 is the row to serialize and Item2 is the destination file path. " +
                    "Got IObservable<" + elementType.Name + "> instead.");
            }

            var rowType = elementType.GetGenericArguments()[0];
            var process = ProcessNamedMethod.MakeGenericMethod(rowType);
            return Expression.Call(Expression.Constant(this), process, source);
        }

        IObservable<T> ProcessNamed<T>(IObservable<Tuple<T, string>> source)
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

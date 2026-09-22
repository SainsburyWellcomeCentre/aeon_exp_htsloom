using Bonsai;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

/// <summary>
/// Copies each list in the sequence into an array. Combinators such as CombineLatest
/// emit IList&lt;T&gt;; visualizers such as RollingGraph want T[]. A plain projection:
/// no subscription is created per element.
/// </summary>
[Combinator]
[Description("Copies each incoming list, or pair of lists, into arrays for consumers that need T[] rather than IList<T>.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class ListToArray
{
    public IObservable<T[]> Process<T>(IObservable<IList<T>> source)
    {
        return source.Select(list => list.ToArray());
    }

    public IObservable<Tuple<T1[], T2[]>> Process<T1, T2>(IObservable<Tuple<IList<T1>, IList<T2>>> source)
    {
        return source.Select(pair => Tuple.Create(pair.Item1.ToArray(), pair.Item2.ToArray()));
    }
}

using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("Simple contains checker")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class Contains
{
    public IObservable<bool> Process<T>(IObservable<Tuple<IList<T>, T>> source)
    {
        return source.Select(value => value.Item1.Contains(value.Item2));
    }
}

using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using HtsLoom;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class KeyValuePairConverter
{
    public IObservable<KeyValuePair<TKey,TValue>> Process<TKey,TValue>(IObservable<Tuple<TValue, TKey>> source)
    {
        return source.Select(value => new KeyValuePair<TKey,TValue>(value.Item2,value.Item1));
    }
}

using Bonsai;
using Bonsai.Harp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

/// <summary>
/// Turns a list of timestamped values that share one timestamp (for example the
/// masked images of every tracking region for a single frame) into one timestamped
/// list. A plain projection: no subscription is created per element.
/// </summary>
[Combinator]
[Description("Packs a list of timestamped values into one timestamped list, keeping the timestamp of the first element.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class PackTimestampedList
{
    public IObservable<Timestamped<IList<T>>> Process<T>(IObservable<IList<Timestamped<T>>> source)
    {
        return source
            .Where(items => items.Count > 0)
            .Select(items => Timestamped.Create(
                (IList<T>)items.Select(item => item.Value).ToList(),
                items[0].Seconds));
    }
}

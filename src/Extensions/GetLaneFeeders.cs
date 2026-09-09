using Bonsai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reactive.Linq;
using HtsLoom;

/// <summary>
/// The distinct feeders a lane (player) drives, taken from the patchStates of
/// its loaded foraging states. Used to scope a lane's per-feeder accumulators
/// to its own feeders instead of every feeder in the rig.
/// </summary>
[Combinator]
[Description("The distinct feeders a lane drives, from the patchStates of its foraging states.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class GetLaneFeeders
{
    public IObservable<IList<FeederName>> Process(IObservable<IDictionary<string, ForagingState>> source)
    {
        return source.Select(states => (IList<FeederName>)states.Values
            .SelectMany(state => state.PatchStates.Keys)
            .Distinct()
            .ToList());
    }
}
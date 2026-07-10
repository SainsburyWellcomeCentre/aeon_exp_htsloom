using Bonsai;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using HtsLoom;

/// <summary>
/// Per-feeder components accumulated while in the current meta-state, published
/// by each running feeder state machine so consumers (logging, visualizers,
/// alerts) can subscribe to exactly the fields they need.
///
/// A baseline instance (Distance = 0, EatenPellets = 0, MissedPellets = 0) is
/// emitted per feeder when the meta-state starts; thereafter it is emitted on
/// each pellet event with the accumulated values.
/// </summary>
public struct FeederInStateReward
{
    public FeederName Feeder { get; set; }
    public string MetaState { get; set; }
    public double Distance { get; set; }
    public double DistanceThreshold { get; set; }
    public int EatenPellets { get; set; }
    public int MissedPellets { get; set; }
}

/// <summary>
/// Maps a race-free per-feeder snapshot tuple
/// ((((deliveredDelta, missedDelta), distance), feeder), metaState) into the
/// named <see cref="FeederInStateReward"/> contract. Pure function of one
/// atomic tuple per emission, so no shared mutable state across the Harp
/// threads. EatenPellets = deliveredDelta - missedDelta; MissedPellets =
/// missedDelta; Distance = meta-state-cumulative wheel displacement.
/// DistanceThreshold is 0 for now.
/// </summary>
[Combinator]
[Description("Maps a per-feeder (((deliveredDelta, missedDelta), distance), feeder), metaState snapshot into a FeederInStateReward.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class CreateFeederInStateReward
{
    public IObservable<FeederInStateReward> Process(
        IObservable<Tuple<Tuple<Tuple<Tuple<int, int>, double>, FeederName>, string>> source)
    {
        return source.Select(x => new FeederInStateReward
        {
            Feeder = x.Item1.Item2,
            MetaState = x.Item2,
            Distance = x.Item1.Item1.Item2,
            DistanceThreshold = 0,
            EatenPellets = x.Item1.Item1.Item1.Item1 - x.Item1.Item1.Item1.Item2,
            MissedPellets = x.Item1.Item1.Item1.Item2,
        });
    }
}
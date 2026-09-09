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
    public string Player { get; set; }
    public FeederName Feeder { get; set; }
    public string MetaState { get; set; }
    public double Distance { get; set; }
    public double DistanceThreshold { get; set; }
    public int EatenPellets { get; set; }
    public int MissedPellets { get; set; }
}

/// <summary>
/// Maps a race-free per-feeder snapshot tuple
/// (((((deliveredDelta, missedDelta), distance), feeder), metaState), player)
/// into the named <see cref="FeederInStateReward"/> contract. Pure function of
/// one atomic tuple per emission, so no shared mutable state across the Harp
/// threads. The Player field disambiguates lanes that share a feeder.
/// EatenPellets = deliveredDelta - missedDelta; MissedPellets = missedDelta;
/// Distance = meta-state-cumulative wheel displacement. DistanceThreshold = 0.
/// </summary>
[Combinator]
[Description("Maps a ((((deliveredDelta, missedDelta), distance), feeder), metaState), player snapshot into a FeederInStateReward.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class CreateFeederInStateReward
{
    public IObservable<FeederInStateReward> Process(
        IObservable<Tuple<Tuple<Tuple<Tuple<Tuple<int, int>, double>, FeederName>, string>, string>> source)
    {
        return source.Select(x =>
        {
            var reward = x.Item1;              // ((((dDelta, mDelta), distance), feeder), metaState)
            var withFeeder = reward.Item1;     // (((dDelta, mDelta), distance), feeder)
            var withDistance = withFeeder.Item1; // ((dDelta, mDelta), distance)
            var deltas = withDistance.Item1;   // (dDelta, mDelta)
            return new FeederInStateReward
            {
                Player = x.Item2,
                Feeder = withFeeder.Item2,
                MetaState = reward.Item2,
                Distance = withDistance.Item2,
                DistanceThreshold = 0,
                EatenPellets = deltas.Item1 - deltas.Item2,
                MissedPellets = deltas.Item2,
            };
        });
    }
}
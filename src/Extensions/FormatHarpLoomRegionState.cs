using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;

[Combinator]
[Description("Transforms a Loom Zone State into a harp message for logging")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class FormatHarpLoomRegionState
{
public int Address { get; set; }
    public IObservable<HarpMessage> Process(IObservable<Timestamped<Tuple<int, int, bool>>> source)
    {
        return source.Select(x =>
            {
                var loomZoneState = x.Value;
                var timestamp = x.Seconds;
                return HarpMessage.FromByte(
                    Address,
                    timestamp,
                    MessageType.Event,

                    //Blob Id
                    (byte)loomZoneState.Item1,
                    //Zone Id
                    (byte)loomZoneState.Item2,
                    //ZoneState
                    (byte)(loomZoneState.Item3?1:0));

            });
    }
}

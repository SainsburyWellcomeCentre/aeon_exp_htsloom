using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;

[Combinator]
[Description("Transforms a Loom Zone Angle into a harp message for logging")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class FormatHarpLoomRegionAngle
{
public int Address { get; set; }
    public IObservable<HarpMessage> Process(IObservable<Timestamped<Tuple<int, int, double>>> source)
    {
        return source.Select(x =>
            {
                var loomZoneAngle = x.Value;
                var timestamp = x.Seconds;
                return HarpMessage.FromSingle(
                    Address,
                    timestamp,
                    MessageType.Event,
                    
                    //Blob Id
                    (float)loomZoneAngle.Item1,
                    //Zone Id
                    (float)loomZoneAngle.Item2,
                    //ZoneState
                    (float)loomZoneAngle.Item3);
                    
            });
    }
}

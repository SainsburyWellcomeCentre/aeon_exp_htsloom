using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;
using Bonsai.Vision;

[Combinator]
[Description("Converts timestamped Indexed HeadTail with region into a sequence of Harp messages.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class IndexedHeadtTailFormatHarp
{
    public int Address { get; set; }
    public IObservable<HarpMessage> Process(IObservable<Timestamped<Tuple<int, HeadTail, ConnectedComponent>>> source)
    {
        return source.Select(x =>
            {
                var tracking = x.Value;
                var timestamp = x.Seconds;
                return HarpMessage.FromSingle(
                    Address,
                    timestamp,
                    MessageType.Event,
                    
                    //Id
                    (float)tracking.Item1,
                    
                    //HeadTail
                    tracking.Item2.Centroid.X,
                    tracking.Item2.Centroid.Y,
                    tracking.Item2.Head.X,
                    tracking.Item2.Head.Y,
                    tracking.Item2.Tail.X,
                    tracking.Item2.Tail.Y,
                    tracking.Item2.Velocity.X,
                    tracking.Item2.Velocity.Y,
                    (float)tracking.Item2.Heading,

                    //ConnectedComponent
                    tracking.Item3.Centroid.X,
                    tracking.Item3.Centroid.Y,
                    (float)tracking.Item3.Orientation,
                    (float)tracking.Item3.MajorAxisLength,
                    (float)tracking.Item3.MinorAxisLength,
                    (float)tracking.Item3.Area);
            });
    }
}

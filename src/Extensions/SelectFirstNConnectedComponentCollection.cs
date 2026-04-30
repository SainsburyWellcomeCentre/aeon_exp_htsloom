using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Vision;
using OpenCV.Net;
using System.Diagnostics.Eventing.Reader;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class SelectFirstNConnectedComponentCollection
{
    public double minArea { get; set; }

    public IObservable<ConnectedComponentCollection> Process(IObservable<ConnectedComponentCollection> source)
    {
        return source.Select(value =>
        {
            if(value.Count <= 0)
            {
                return value;
            }
            var imageSize = value.ImageSize;
            ConnectedComponentCollection result = new ConnectedComponentCollection(imageSize);

            if (value.Count > 1)
            {
                if (value[0].Area > minArea && value[1].Area > 200)
                    result.Add((value[0].Centroid.Y < value[1].Centroid.Y) ? value[0] : value[1]);
                else
                    result.Add(value[0]);
            }
            else
                result.Add(value[0]);
            return result;
        });
    }
}

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
[Description("From the two first elements in the collection select the top one if both are bigger than MinAera")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class SelectTopComponent
{
    public double MinArea { get; set; }

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
                if (value[0].Area > MinArea && value[1].Area > MinArea)
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

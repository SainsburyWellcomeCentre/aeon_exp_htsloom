using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using HtsLoom;
using OpenCV.Net;

[Combinator]
[Description("Convert HtsLoom point to OpenCV Point2f")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class ConvertToPoint2f
{
    public IObservable<Point2f> Process(IObservable<HtsLoom.Point> source)
    {
        return source.Select(value => new Point2f(value.X, value.Y));
    }
}

using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

[Combinator]
[Description("Calculate angle of a line segment defined by 2 points")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class AngleCalculator
{

    public IObservable<double> Process(IObservable<Tuple<Point2f, Point2f>> source)
    {
        return source.Select(value => 
        {
            var subtract = value.Item2-value.Item1;
            var heading = Math.Atan2(-subtract.Y,subtract.X);
            return heading;
        });
    }
}

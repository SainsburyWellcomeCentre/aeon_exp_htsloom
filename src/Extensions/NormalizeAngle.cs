using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("Normalize the angle to be within [-π, π]")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class NormalizeAngle
{
    public IObservable<double> Process(IObservable<double> source)
    {
        return source.Select(value => Normalize(value));
    }
    private double Normalize(double angle)
    {
        // Normalize the angle to be within [-π, π]
        angle = (angle + Math.PI) % (2 * Math.PI);  // Shift by π and take modulo 2π
        if (angle < 0)
        {
            angle += 2 * Math.PI;  // Ensure the result is positive
        }
        return angle - Math.PI;  // Shift back by π to return to [-π, π)
    }
}

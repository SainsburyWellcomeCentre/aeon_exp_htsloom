using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using Bonsai.Numerics;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class Threshold
{
    /// <summary>
    /// Gets or sets the value assigned to pixels determined to be above the threshold.
    /// </summary>
    [Description("The value assigned to pixels determined to be above the threshold.")]
    public double MaxValue { get; set; } 

    /// <summary>
    /// Gets or sets the type of threshold to apply to individual pixels.
    /// </summary>
    [Description("The type of threshold to apply to individual pixels.")]
    public ThresholdTypes ThresholdType { get; set; }

    public Threshold()
    {
        MaxValue = 255;
    }

    public IObservable<IplImage> Process(IObservable<Tuple<IplImage, int>> source)
    {
        return source.Select(value =>
        {
            var input = value.Item1;
            if (input.Depth == IplDepth.U16)
            {
                var temp = new IplImage(input.Size, IplDepth.F32, input.Channels);
                CV.Convert(input, temp);
                input = temp;
            }

            var output = new IplImage(input.Size, IplDepth.U8, input.Channels);
            CV.Threshold(input, output, value.Item2, MaxValue, ThresholdType);
            return output;
            
        });
    }
}

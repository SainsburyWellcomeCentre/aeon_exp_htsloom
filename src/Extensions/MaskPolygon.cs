using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using System.Runtime.InteropServices;


[Combinator]
[Description("Applies a polygonal mask to each image in the sequence.")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class MaskPolygon
{
    /// <summary>
    /// Gets or sets a value specifying the type of mask operation to apply
    /// on the region of interest.
    /// </summary>
    [TypeConverter(typeof(ThresholdTypeConverter))]
    [Description("Specifies the type of mask operation to apply on the region of interest.")]
    public ThresholdTypes MaskType { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="Scalar"/> specifying the value to which all
    /// pixels that are not in the selected region will be set to.
    /// </summary>
    [Description("Specifies the value to which all pixels that are not in the selected region will be set to.")]
    public Scalar FillValue { get; set; }

    public MaskPolygon()
    {
        MaskType = ThresholdTypes.ToZero;
    }
    Rect ClipRectangle(Rect rect, Size clipSize)
    {
        var clipX = rect.X < 0 ? -rect.X : 0;
        var clipY = rect.Y < 0 ? -rect.Y : 0;
        clipX += Math.Max(0, rect.X + rect.Width - clipSize.Width);
        clipY += Math.Max(0, rect.Y + rect.Height - clipSize.Height);

        rect.X = Math.Max(0, rect.X);
        rect.Y = Math.Max(0, rect.Y);
        rect.Width = rect.Width - clipX;
        rect.Height = rect.Height - clipY;
        return rect;
    }
    public IObservable<IplImage> Process(IObservable<Tuple<IplImage, Point[][]>> source)
    {
        return Observable.Defer(() =>
        {
            var mask = default(IplImage);
            var boundingBox = default(Rect);

            return source.Select(input =>
            {
                var currentRegions = input.Item2;
                if (currentRegions != null)
                {
                    mask = new IplImage(input.Item1.Size, IplDepth.U8, 1);
                    mask.SetZero();

                    var points = currentRegions
                        .SelectMany(region => region)
                        .SelectMany(point => new[] { point.X, point.Y })
                        .ToArray();
                    if (points.Length > 0)
                    {
                        using (var mat = new Mat(1, points.Length / 2, Depth.S32, 2))
                        {
                            Marshal.Copy(points, 0, mat.Data, points.Length);
                            boundingBox = CV.BoundingRect(mat);
                            boundingBox = ClipRectangle(boundingBox, input.Item1.Size);
                        }

                        CV.FillPoly(mask, currentRegions, Scalar.All(255));
                    }
                }
                else
                    mask = null;

                var selectionType = MaskType;
                if (selectionType <= ThresholdTypes.BinaryInv)
                {
                    var size = input.Item1.Size;
                    var output = new IplImage(size, IplDepth.U8, 1);
                    switch (selectionType)
                    {
                        case ThresholdTypes.Binary:
                            if (mask == null) output.SetZero();
                            else CV.Copy(mask, output);
                            break;
                        case ThresholdTypes.BinaryInv:
                            if (mask == null) output.Set(Scalar.All(255));
                            else CV.Not(mask, output);
                            break;
                        default:
                            throw new InvalidOperationException("Selection operation is not supported.");
                    }

                    return output;
                }

                if (currentRegions != null && boundingBox.Width > 0 && boundingBox.Height > 0)
                {
                    var output = new IplImage(input.Item1.Size, input.Item1.Depth, input.Item1.Channels);

                    switch (selectionType)
                    {
                        case ThresholdTypes.ToZeroInv:

                            CV.Copy(input.Item1, output);
                            output.Set(FillValue, mask);
                            break;
                        case ThresholdTypes.ToZero:
                            output.Set(FillValue);
                            CV.Copy(input.Item1, output, mask);
                            break;
                        default:
                            throw new InvalidOperationException("Selection operation is not supported.");
                    }

                    return output;
                }

                return input.Item1;
            });
        });
    }
    class ThresholdTypeConverter : EnumConverter
    {
        public ThresholdTypeConverter(Type type)
            : base(type)
        {
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(
                base.GetStandardValues(context)
                .Cast<ThresholdTypes>()
                .Where(type => type != ThresholdTypes.Truncate &&
                                type != ThresholdTypes.Otsu)
                .ToArray());
        }
    }
}
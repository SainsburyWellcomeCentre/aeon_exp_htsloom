using Bonsai;
using Bonsai.Harp;
using Bonsai.Reactive;
using Bonsai.Vision;
using OpenCV.Net;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

[Combinator]
    [Description("Generates boolean values indicating whether each point in the sequence is inside a region of interest.")]
    public class RegionContainsPoint
    {
        [Description("The array of vertices specifying the region of interest.")]
        [Editor("Bonsai.Vision.Design.IplImageInputRoiEditor, Bonsai.Vision.Design", DesignTypes.UITypeEditor)]
        public Point[][] Regions { get; set; }

        static bool Contains(Point[][] contour, Point2f point)
        {
            if (contour == null) return false;
            for (int i = 0; i < contour.Length; i++)
            {
                using (var contourHeader = Mat.CreateMatHeader(contour[i], contour[i].Length, 2, Depth.S32, 1))
                {
                    if (CV.PointPolygonTest(contourHeader, point, false) > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IObservable<Timestamped<bool>> Process(IObservable<Timestamped<Point2f>> source)
        {
            // var points = Regions.SelectMany(region => region.SelectMany(point => new Point(point.X, point.Y))).ToArray();
            // .SelectMany(region => region)
            // .SelectMany(point => new[] { point.X, point.Y })
            // .ToArray();
            // Console.WriteLine("Here");
            // var points = Regions.ToObservable() // Convert outer array to observable sequence
            //     .Select(innerArray =>
            //         innerArray.ToArray() // Clone inner array (shallow copy of elements)
            //     )
            //     .ToArray() // Convert back to array of arrays
            //     .Wait();   // Wait for observable to complete and get result
            // Console.WriteLine("Here2");
            var points = new Point[(Regions.Length)][];
            var index =0;
            foreach (var setOfPoints in Regions)
            {
                points[index] = new Point[setOfPoints.Length];
                int count =0;
                foreach(var point in setOfPoints)
                {
                    points[index][count] = new Point(point.X,point.Y);
                    count++;
                }
                index++;
            }
                    
            return source.Select(x =>
            {
                var containsPoint = Contains(points, x.Value);
                return Timestamped.Create(containsPoint, x.Seconds);
            });
        }

    }
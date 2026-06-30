using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

[Combinator]
[Description("Creates a HeadTail data type from two region extremes and a centroid history used to compute current velocity.")]
[WorkflowElementCategory(ElementCategory.Transform)]
[TypeVisualizer("Bonsai.Vision.Design.BinaryRegionExtremesVisualizer, Bonsai.Vision.Design")]
public class HeadTailVelocity
{
    public double VelocityThreshold { get; set; }

    static double Norm(Point2f v)
    {
        return Math.Sqrt(v.X * v.X + v.Y * v.Y);
    }

    static double Dot(Point2f a, Point2f b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    public IObservable<HeadTail> Process(IObservable<Tuple<Point2f, Point2f, IList<Point2f>, double>> source)
    {
        return ProcessCore(source);
    }

    public IObservable<HeadTail> Process(IObservable<Tuple<Point2f, Point2f, IList<Point2f>, int>> source)
    {
        return ProcessCore(source.Select(value => Tuple.Create(value.Item1, value.Item2,value.Item3, (double)VelocityThreshold)));
    }

    public IObservable<HeadTail> Process(IObservable<Tuple<Point2f, Point2f, IList<Point2f>>> source)
    {
        return ProcessCore(source.Select(value => Tuple.Create(value.Item1, value.Item2,value.Item3, VelocityThreshold)));
    }

    IObservable<HeadTail> ProcessCore(IObservable<Tuple<Point2f, Point2f, IList<Point2f>, double>> source)
    {
        // Defer so each subscription gets its own oldHead/oldTail state.
        return Observable.Defer(() =>
        {
            Point2f oldHead = new Point2f();
            Point2f oldTail = new Point2f();
            return source.Select(value =>
            {
                var p1 = value.Item1;
                var p2 = value.Item2;
                var centroids = value.Item3;
                var threshold = value.Item4;

                // Assign current extremes to head/tail by minimum total distance to previous frame.
                Point2f head, tail;
                if (Norm(p1 - oldHead) + Norm(p2 - oldTail) <
                    Norm(p1 - oldTail) + Norm(p2 - oldHead))
                {
                    head = p1; tail = p2;
                }
                else
                {
                    head = p2; tail = p1;
                }

                // Net displacement across the centroid history.
                var distance = new Point2f(0, 0);
                for (int i = 1; i < centroids.Count; i++)
                {
                    distance += centroids[i] - centroids[i - 1];
                }

                // If motion is strong and points against head-tail, flip them.
                if (Norm(distance) > threshold && Dot(distance, head - tail) < 0)
                {
                    var tmp = head; head = tail; tail = tmp;
                }

                oldHead = head;
                oldTail = tail;

                var d = head - tail;
                return new HeadTail
                {
                    Head = head,
                    Tail = tail,
                    Heading = Math.Atan2(-d.Y, d.X),
                    Centroid = centroids.Last(),
                    Velocity = distance
                };
            });
        });
    }
}

public struct HeadTail
{
    public Point2f Head;
    public Point2f Tail;
    public double Heading;
    public Point2f Centroid;
    public Point2f Velocity;
}

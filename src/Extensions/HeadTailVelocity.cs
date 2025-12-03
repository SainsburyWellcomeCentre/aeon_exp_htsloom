using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
/*
[assembly:TypeVisualizer("Bonsai.Vision.Design.BinaryRegionExtremesVisualizer, Bonsai.Vision.Design", TargetType= typeof(Tuple<Point2f,Point2f>))]
*/
[Combinator]
[Description("Creates a HeadTail data type from two points(region extremes) and a list of points (Centroids) used has history to calculate current velocity Tuple<Tuple<Point2f, Point2f>, IList<Point2f>>")]
[WorkflowElementCategory(ElementCategory.Transform)]
[TypeVisualizer("Bonsai.Vision.Design.BinaryRegionExtremesVisualizer, Bonsai.Vision.Design")]
public class HeadTailVelocity
{
    public double VelocityThreshold { get; set; }
  

    
    double  Norm(Point2f value)
    {
        return Math.Sqrt((value.X*value.X)+(value.Y*value.Y));
    }
    double  DotProduct(Point2f value1, Point2f value2)
    {
        return (value1.X*value2.X)+(value1.Y*value2.Y);
    }
    public IObservable<HeadTail> Process(IObservable<Tuple<Tuple<Point2f, Point2f>, IList<Point2f>>> source)
    {
        Point2f oldHead = new Point2f();
        Point2f oldTail = new Point2f();
        return source.Select(value => 
        {
            Point2f head, tail;
            var dist11 = Norm(value.Item1.Item1 - oldHead) + Norm(value.Item1.Item2 - oldTail);
            var dist12 = Norm(value.Item1.Item1 - oldTail) + Norm(value.Item1.Item2 - oldHead);
            
            if(dist11<dist12 )
            {
               head = value.Item1.Item1;
               tail = value.Item1.Item2; 
            }
          
            else //if(dist21>dist12 && dist21>dist11 && dist21>dist22)
            {
                head = value.Item1.Item2;
                tail = value.Item1.Item1;
            }
            Point2f distance = new Point2f(0,0);
            for(int i=1; i <value.Item2.Count; i++)
            {
                distance += value.Item2[i]-value.Item2[i-1];
            }
            var res = Norm(distance);
            if (res > VelocityThreshold)
            {
                if(DotProduct(distance, head-tail)<0)
                {
                    var tmp = head;
                    head= tail;
                    tail = tmp;
                }
            }

            //else
            oldHead = head;
            oldTail = tail;
            var subtract = head-tail;
            var heading = Math.Atan2(-subtract.Y,subtract.X);
            return new HeadTail{Head = head, Tail= tail, Heading= heading, Centroid= value.Item2.Last(), Velocity = distance};
            // var centerA = (value.Item1.Item1+value.Item1.Item2)*0.5f;
            // //var ExtremeA2 = value.Item1.Item2;
            // var centerB = (value.Item2.Item1+value.Item2.Item2)*0.5f;
            // var originVector = centerB - centerA;
            // var orientation = Math.Atan2(originVector.X,originVector.Y);
            // return new Tuple<Point2f, Point2f>(centerA,centerB);
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

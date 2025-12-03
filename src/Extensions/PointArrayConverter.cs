using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using Bonsai.Reactive;
using System.Windows.Markup;

[Combinator]
[Description("Converts Point[][] to List<List<HtsLoom.Point>> and List<List<HtsLoom.Point>> to Point[][]")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class PointArrayConverter
{
    public IObservable<List<HtsLoom.Polygon>> Process(IObservable<Point[][]> source)
    {
        return source.Select(value =>
        {
            var finalList = new List<HtsLoom.Polygon>();
            foreach (var list in value)
            {
                var newList = new HtsLoom.Polygon();
                foreach (var item in list)
                {
                    newList.Points.Add(new HtsLoom.Point() { X = item.X, Y = item.Y });
                }
                finalList.Add(newList);

            }
            return finalList;
        });
    }
    public IObservable<Point[][]> Process( IObservable<List<HtsLoom.Polygon>> source)
    {
        return source.Select(value =>
        {
            Point[][] finalPoints = new Point[value.Count][];
            var mainIndex = 0;
            foreach (var polygon in value)
            {
                var list = polygon.Points;
                finalPoints[mainIndex] = new Point[list.Count];
                var index = 0;
                foreach (var item in list)
                {
                    finalPoints[mainIndex][index] = new Point(item.X, item.Y);
                    index++;
                }
                mainIndex++;
            }
            return finalPoints;
        });
    }
}

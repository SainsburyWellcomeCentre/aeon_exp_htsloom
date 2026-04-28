using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Vision;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class SelectFirstNConnectedComponentCollection
{
    public int NumberOfElements { get; set; }
    public IObservable<ConnectedComponentCollection> Process(IObservable<ConnectedComponentCollection> source)
    {
        return source.Select(value =>
        {
            var imageSize = value.ImageSize;
            ConnectedComponentCollection result = new ConnectedComponentCollection(imageSize);
            var max = (value.Count > NumberOfElements)?NumberOfElements: value.Count; 
            for(int count=0; count < max; count++)
                result.Add(value[count]);
            return result   ;
        });
    }
}

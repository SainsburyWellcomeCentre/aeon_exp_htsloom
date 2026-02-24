using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;

[Combinator]
[Description("Convert Any type to object")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class ConvertToObject
{
    public IObservable<object> Process(IObservable<object> source)
    {
        return source;
    }
}


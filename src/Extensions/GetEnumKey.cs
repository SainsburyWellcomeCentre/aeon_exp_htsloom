using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("Retrieves the string name from enum given a instance of an enum type")]
[WorkflowElementCategory(ElementCategory.Transform)]
public class GetEnumKey
{
    public IObservable<string>Process<T>(IObservable<T> source) 
    {
        var type = typeof(T);
        if (!type.IsEnum) 
        {
            throw new ArgumentException("Input must be an enumerated type");
        }
        return source.Select(value => Enum.GetName(type,value));
    }
}
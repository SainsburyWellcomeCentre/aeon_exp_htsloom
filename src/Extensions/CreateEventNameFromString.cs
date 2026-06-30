using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using HtsLoom;

[Combinator]
[Description("Create an instance of EventName based on the string parameter")]
[WorkflowElementCategory(ElementCategory.Source)]
public class CreateEventNameFromString
{
    public string Name {get; set;}
    
    EventName CreateEventNameEnum(string name)
    {
        return (EventName) Enum.Parse( typeof(EventName), name);
    }

    public IObservable<EventName> Process(IObservable<object> source)
    {
        var name = Name;
        return source.Select(value =>
        {
            return CreateEventNameEnum(name);
        });
    }
}

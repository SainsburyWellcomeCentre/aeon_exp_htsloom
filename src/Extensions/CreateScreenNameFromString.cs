using Bonsai;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using HtsLoom;

[Combinator]
[Description("Create an instance of ScreenName based on the string parameter")]
[WorkflowElementCategory(ElementCategory.Source)]
public class CreateScreenNameFromString
{
    public string Name {get; set;}
    
    ScreenName CreateScreenNameEnum(string name)
    {
        return (ScreenName) Enum.Parse( typeof(ScreenName), name);
    }

    public IObservable<ScreenName> Process(IObservable<object> source)
    {
        var name = Name;
        return source.Select(value =>
        {
            return CreateScreenNameEnum(name);
        });
    }
}

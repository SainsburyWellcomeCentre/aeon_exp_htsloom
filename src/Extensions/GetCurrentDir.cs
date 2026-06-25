using Bonsai;
using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

[Combinator]
[Description("")]
[WorkflowElementCategory(ElementCategory.Source)]
public class GetCurrentDir
{
    public IObservable<string> Process()
    {

        return Observable.Defer(() => Observable.Return(Directory.GetCurrentDirectory()));
    }
}

﻿using System;
using System.Windows.Markup;

var x = 1;
public class MyExtension3 : MarkupExtension
{
    public MyExtension3(object value1) { Value1 = value1; }

    [ConstructorArgument("value2")]  // Noncompliant {{Change this 'ConstructorArgumentAttribute' value to match one of the existing constructors arguments.}}
    public object Value1 { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => null;
}

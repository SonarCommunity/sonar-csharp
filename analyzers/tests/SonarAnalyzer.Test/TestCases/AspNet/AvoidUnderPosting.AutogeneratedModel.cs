﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Repro https://github.com/SonarSource/sonar-dotnet/issues/9260
// Also related to https://github.com/SonarSource/sonar-dotnet/issues/8876
public partial class AutogeneratedModel
{
    public int Property  // Noncompliant, FP - we should not raise in autogenerated classes.
    {
        get;
        set;
    }
}

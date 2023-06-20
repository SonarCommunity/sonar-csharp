﻿Imports System
Imports System.Globalization

Public Class DateTimeFormatShouldNotBeHardcoded
    Public Sub DateTimeCases()
        Dim StringRepresentation = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss") ' Noncompliant {{Use the CultureInfo class to provide the format for the string.}}
        '                                          ^^^^^^^^
        StringRepresentation = DateTime.UtcNow.ToString("dd/mm/yyyy HH:MM:ss") ' Noncompliant
        StringRepresentation = DateTime.UtcNow.ToString(CultureInfo.GetCultureInfo("es-MX"))
        StringRepresentation = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
    End Sub

    Public Sub DateTimeOffsetCases(ByVal DateTimeOffset As DateTimeOffset)
        Dim StringRepresentation = DateTimeOffset.ToString("dd/MM/yyyy HH:mm:ss") ' Noncompliant
        StringRepresentation = DateTimeOffset.ToString("dd/mm/yyyy HH:MM:ss") ' Noncompliant
        StringRepresentation = DateTimeOffset.ToString(CultureInfo.GetCultureInfo("es-MX"))
        StringRepresentation = DateTimeOffset.ToString(CultureInfo.InvariantCulture)
    End Sub

    Public Sub DateOnlyCases(ByVal DateOnly As DateOnly)
        Dim StringRepresentation = DateOnly.ToString("dd/MM/yyyy HH:mm:ss") ' Noncompliant
        StringRepresentation = DateOnly.ToString("dd/mm/yyyy HH:MM:ss") ' Noncompliant
        StringRepresentation = DateOnly.ToString(CultureInfo.GetCultureInfo("es-MX"))
        StringRepresentation = DateOnly.ToString(CultureInfo.InvariantCulture)
    End Sub

    Public Sub TimeOnlyCases(ByVal TimeOnly As TimeOnly)
        Dim StringRepresentation = TimeOnly.ToString("dd/MM/yyyy HH:mm:ss") ' Noncompliant
        StringRepresentation = TimeOnly.ToString("dd/mm/yyyy HH:MM:ss") ' Noncompliant
        StringRepresentation = TimeOnly.ToString(CultureInfo.GetCultureInfo("es-MX"))
        StringRepresentation = TimeOnly.ToString(CultureInfo.InvariantCulture)
    End Sub
End Class

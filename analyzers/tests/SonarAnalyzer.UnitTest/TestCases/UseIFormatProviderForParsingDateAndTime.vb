﻿Imports System.Globalization
Imports System.DateTime

Class Test
    ReadOnly formatProviderField As IFormatProvider = New CultureInfo("en-US")

    Sub DifferentSyntaxScenarios()
        Dim dt = Date.Parse("01/02/2000")                                   ' Noncompliant
        '        ^^^^^^^^^^^^^^^^^^^^^^^^
        Date.Parse("01/02/2000")                                            ' Noncompliant

        Parse("01/02/2000")                                                 ' Noncompliant {{Pass an 'IFormatProvider' to the 'DateTime.Parse' method.}}
        System.DateTime.Parse("01/02/2000")                                 ' Noncompliant {{Pass an 'IFormatProvider' to the 'DateTime.Parse' method.}}

        Dim parsedDate As Date = Nothing

        If Date.TryParse("01/02/2000", parsedDate) Then                     ' Noncompliant
        End If

        dt = Date.Parse("01/02/2000").AddDays(1)                            ' Noncompliant

        Dim parsedDates = {"01/02/2000"}.Select(Function(x) Date.Parse(x))  ' Noncompliant
    End Sub

    Sub CallWithNullIFormatProvider()
        Date.Parse("01/02/2000", Nothing)                                               ' Noncompliant
        Date.Parse("01/02/2000", Nothing, DateTimeStyles.None)                          ' Noncompliant

        Date.Parse("01/02/2000", Nothing)                                               ' FN
        Date.Parse("01/02/2000", If(True, CType(Nothing, IFormatProvider), Nothing))    ' FN

        Dim nullFormatProvider As IFormatProvider = Nothing
        Date.Parse("01/02/2000", nullFormatProvider)                                    ' FN
    End Sub

    Sub CallWithDeterministicFormatProviders()
        Date.Parse("01/02/2000", CultureInfo.InvariantCulture)                 ' Compliant
        Date.Parse("01/02/2000", CultureInfo.GetCultureInfo("en-US"))          ' Compliant
        Date.Parse("01/02/2000", formatProviderField)                          ' Compliant
        Date.Parse("01/02/2000", formatProviderField)                          ' Compliant
    End Sub

    Sub CallWithNonDeterministicFormatProviders()
        Date.Parse("01/02/2000", CultureInfo.CurrentCulture)                   ' Noncompliant
        Date.Parse("01/02/2000", CultureInfo.CurrentUICulture)                 ' Noncompliant
        Date.Parse("01/02/2000", CultureInfo.DefaultThreadCurrentCulture)      ' Noncompliant
        Date.Parse("01/02/2000", CultureInfo.DefaultThreadCurrentUICulture)    ' Noncompliant
    End Sub

    Sub ParseMethodsOfNonTemporalTypes()
        Integer.Parse("1")                                                      ' Compliant - this rule only deals with temporal types
        Dim parsedDouble = Nothing
        Double.TryParse("1.1", parsedDouble)
    End Sub
End Class

Class CustomTypeCalledDateTime
    Public Structure DateTime
        Public Shared ReadOnly Property Now As DateTime
            Get
                Return New DateTime()
            End Get
        End Property
    End Structure

    Sub New()
        Dim currentTime = DateTime.Now                                          ' Compliant - this is not System.DateTime
    End Sub
End Class

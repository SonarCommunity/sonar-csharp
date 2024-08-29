Imports System.ComponentModel.DataAnnotations
Imports System.Text.RegularExpressions

Class Compliant
    Private Sub Ctor()
        Dim defaultOrder = New Regex("single space")
        Dim namedArgs = New Regex(options:=RegexOptions.IgnorePatternWhitespace, pattern:="ignore  pattern  white space")
    End Sub

    Private Sub [Static]()
        Dim isMatch = Regex.IsMatch("some input", "single space")
    End Sub

    <RegularExpression("single space")>
    Public Property Attribute As String
End Class

Class Noncompliant
    Private Sub Ctor()
        Dim patternOnly = New Regex("multiple  white      spaces") ' Noncompliant {{Regular expressions should not contain multiple spaces.}}
        '                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    End Sub

    Private Sub [Static]()
        Dim isMatch = Regex.IsMatch("some input", "multiple  white  spaces") ' Noncompliant
        Dim match = Regex.Match("some input", "multiple  white  spaces")     ' Noncompliant
        Dim matches = Regex.Matches("some input", "multiple  white  spaces") ' Noncompliant
        Dim split = Regex.Split("some input", "multiple  white  spaces")     ' Noncompliant
        Dim replace = Regex.Replace("some input", "multiple  white  spaces", "some replacement") ' Noncompliant
    End Sub

    <RegularExpression("multiple  white  spaces")> ' Noncompliant
    Public Property Attribute As String
End Class

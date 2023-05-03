Imports System.Collections.Generic
Imports System.Linq
Imports System
Imports System.ComponentModel

Module HelperClass
    Function DoWorkReturnGroup() As List(Of Integer)
        Return Nothing
    End Function

    Sub DoWorkMethodGroup(Of T)(firstOrDefault As Func(Of Func(Of T, Boolean), T))
    End Sub
End Module

Public Class UseFind
    Public Class Dummy
        Public Function FirstOrDefault() As Object
            Return Nothing
        End Function

        Public Function FirstOrDefault(predicate As Func(Of Integer, Boolean)) As Object
            Return Nothing
        End Function
    End Class

    Public Class AnotherDummy
        Public ReadOnly Property FirstOrDefault As Object
            Get
                Return Nothing
            End Get
        End Property
    End Class

    Public Class MyList
        Inherits List(Of Integer)
    End Class

    Public Class HiddenList
        Inherits List(Of Integer)

        Public Function FirstOrDefault(predicate As Func(Of Integer, Boolean)) As Integer?
            Return Nothing
        End Function
    End Class

    Public Sub UnrelatedType(ByVal dummy As Dummy, ByVal anotherDummy As AnotherDummy)
        Dim unused = dummy.FirstOrDefault() ' Compliant
        unused = dummy.FirstOrDefault(Function(x) True) ' Compliant

        unused = anotherDummy.FirstOrDefault
    End Sub

    Public Sub ListBasic(ByVal data As List(Of Integer))
        Dim unused = data.FirstOrDefault(Function(x) True) ' Noncompliant
'                         ^^^^^^^^^^^^^^
        unused = data.Find(Function(x) True) ' Compliant
    End Sub

    Public Sub ThroughLinq(data As List(Of Integer))
        data.[Select](Function(x) x * 2).ToList().FirstOrDefault(Function(x) True) ' Noncompliant {{"Find" method should be used instead of the "FirstOrDefault" extension method.}}
'                                                 ^^^^^^^^^^^^^^
        data.[Select](Function(x) x * 2).ToList().Find(Function(x) True) ' Compliant
    End Sub

    Public Sub ThroughFunction()
        Dim unused = HelperClass.DoWorkReturnGroup().FirstOrDefault(Function(x) True) ' Noncompliant
'                                                    ^^^^^^^^^^^^^^
        unused = HelperClass.DoWorkReturnGroup().Find(Function(x) True) ' Compliant
    End Sub

    Public Sub ThroughLambda(lambda As Func(Of List(Of Integer)))
        Dim unused = lambda().FirstOrDefault(Function(x) True) ' Noncompliant
'                             ^^^^^^^^^^^^^^
        unused = lambda().Find(Function(x) True) ' Compliant
    End Sub

    Public Sub WithinALambda()
        Dim unused = New Func(Of List(Of Integer), Integer)(Function(list) list.FirstOrDefault(Function(x) True)) ' Noncompliant
'                                                                               ^^^^^^^^^^^^^^
    End Sub

    Public Sub AsMethodGroup(data As List(Of Integer))
        HelperClass.DoWorkMethodGroup(Of Integer)(AddressOf data.FirstOrDefault) ' Noncompliant
'                                                                ^^^^^^^^^^^^^^
    End Sub
End Class

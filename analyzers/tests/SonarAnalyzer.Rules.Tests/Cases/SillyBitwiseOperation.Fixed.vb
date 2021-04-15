﻿Imports System

Public Enum Types
    [Class] = 0
    Struct = 1
    [Public] = 2
    [Private] = 4
End Enum

Class SillyBitwiseOperation

    Public Sub Method()
        Dim Result As Integer
        Dim Zero As Integer = 0
        Dim One As Integer = 1
        Dim BitMask As Integer = &H10F

        Result = BitMask ' Fixed
        Result = BitMask   ' Fixed
        Result = BitMask  ' Fixed
        Result = BitMask  ' Fixed
        Result = BitMask   ' Fixed

        Result = BitMask And 1  ' Compliant
        Result = BitMask Or 1   ' Compliant
        Result = BitMask Xor 1  ' Compliant
        Result = BitMask Xor One    ' Compliant

        Dim BitMaskLong As Long = &H10F
        Dim ResultLong As Long
        ResultLong = BitMaskLong ' Fixed
        ResultLong = BitMaskLong And 0L     ' Compliant
        ResultLong = BitMaskLong     ' Fixed
        ResultLong = BitMaskLong    ' Fixed
        ResultLong = BitMaskLong And ReturnLong() ' Compliant
        ResultLong = BitMaskLong And &HF   ' Compliant

        Dim ResultULong As ULong = 1UL     ' Fixed
        ResultULong = 1UL Or 18446744073709551615UL ' Compliant

        MyMethod(1UL) ' Fixed

        If True AndAlso True Then Return
        If True OrElse True Then Return

        Dim Flags As Types = Types.Class Or Types.Private   ' Compliant even when class is zero
        Flags = Types.Class
        Flags = Flags Or Types.Private      ' Compliant, even when "flags" was initally zero
    End Sub

    Private Shared Function ReturnLong() As Long
        Return 1L
    End Function

    Private Shared Sub MyMethod(U As UInt64)
    End Sub

End Class

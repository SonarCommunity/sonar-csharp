﻿Imports System
Imports System.Globalization

Public Class Program
    Public Sub DateTimeTest(ByVal ticks As Integer, ByVal year As Integer, ByVal month As Integer, ByVal day As Integer, ByVal hour As Integer, ByVal minute As Integer, ByVal second As Integer, ByVal millisecond As Integer, ByVal microsecond As Integer, ByVal calendar As Calendar, ByVal kind As DateTimeKind)
        ' default date
        Dim ctor0_0 = New DateTime() ' Compliant

        ' ticks
        Dim ctor1_0 = New DateTime(1) ' Compliant
        Dim ctor1_1 = New DateTime(ticks) ' Compliant
        Dim ctor1_2 = New DateTime(ticks:=ticks)

        ' year, month, and day
        Dim ctor2_0 = New DateTime(1, 1, 1) ' Compliant
        Dim ctor2_1 = New DateTime(1, 3, 1) ' Compliant
        Dim ctor2_2 = New DateTime(year, month, day) ' Compliant
        Dim ctor2_3 = New DateTime(month:=month, day:=day, year:=year) ' Compliant
        Dim ctor2_4 = New DateTime(month:=1, day:=3, year:=1) ' Compliant

        ' year, month, day, and calendar
        Dim ctor3_0 = New DateTime(1, 1, 1, New GregorianCalendar()) ' Compliant
        Dim ctor3_1 = New DateTime(1, 3, 1, New GregorianCalendar()) ' Compliant
        Dim ctor3_2 = New DateTime(year, month, day, calendar) ' Compliant
        Dim ctor3_3 = New DateTime(month:=month, day:=day, calendar:=calendar, year:=year) ' Compliant

        ' year, month, day, hour, minute, and second
        Dim ctor4_0 = New DateTime(1, 1, 1, 1, 1, 1) ' Compliant
        Dim ctor4_1 = New DateTime(1, 3, 1, 1, 1, 1) ' Compliant
        Dim ctor4_2 = New DateTime(1, 1, 1, 1, 3, 1) ' Compliant
        Dim ctor4_3 = New DateTime(year, month, day, hour, minute, second) ' Compliant
        Dim ctor4_4 = New DateTime(year:=year, minute:=1, month:=1, day:=1, hour:=1, second:=second) ' Compliant
        Dim ctor4_5 = New DateTime(year:=1, minute:=3, month:=1, day:=1, hour:=1, second:=4) ' Compliant
        Dim ctor4_6 = New DateTime(year:=1, minute:=minute, month:=1, day:=1, hour:=hour, second:=4) ' Compliant

        ' year, month, day, hour, minute, second, and calendar
        Dim ctor5_0 = New DateTime(1, 1, 1, 1, 1, 1, New GregorianCalendar()) ' Compliant
        Dim ctor5_1 = New DateTime(1, 3, 1, 1, 1, 1, New GregorianCalendar()) ' Compliant

        ' year, month, day, hour, minute, second, and DateTimeKind value
        Dim ctor6_0 = New DateTime(1, 1, 1, 1, 1, 1, DateTimeKind.Utc) ' Compliant
        Dim ctor6_1 = New DateTime(1, 3, 1, 1, 1, 1, DateTimeKind.Utc) ' Compliant

        ' year, month, day, hour, minute, second, and millisecond
        Dim ctor7_0 = New DateTime(1, 1, 1, 1, 1, 1, 1) ' Compliant
        Dim ctor7_1 = New DateTime(1, 1, 1, 1, 1, 1, 3) ' Compliant
        Dim ctor7_2 = New DateTime(1, 3, 1, 1, 1, 1, 1) ' Compliant
        Dim ctor7_3 = New DateTime(year, month, day, hour, minute, second, millisecond) ' Compliant
        Dim ctor7_4 = New DateTime(year:=year, minute:=1, month:=1, day:=1, hour:=1, millisecond:=1, second:=second) ' Compliant
        Dim ctor7_5 = New DateTime(year:=1, minute:=3, month:=1, day:=1, hour:=1, millisecond:=1, second:=4) ' Compliant
        Dim ctor7_6 = New DateTime(year:=1, minute:=minute, month:=1, day:=1, hour:=hour, millisecond:=millisecond, second:=4) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, and calendar
        Dim ctor8_0 = New DateTime(1, 1, 1, 1, 1, 1, 1, New GregorianCalendar()) ' Compliant
        Dim ctor8_1 = New DateTime(1, 3, 1, 1, 1, 1, 1, New GregorianCalendar()) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, and DateTimeKind value
        Dim ctor9_0 = New DateTime(1, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc) ' Compliant
        Dim ctor9_1 = New DateTime(1, 3, 1, 1, 1, 1, 1, DateTimeKind.Utc) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, calendar and DateTimeKind value
        Dim ctor10_0 = New DateTime(1, 1, 1, 1, 1, 1, 1, New GregorianCalendar(), DateTimeKind.Utc) ' Compliant
        Dim ctor10_1 = New DateTime(1, 3, 1, 1, 1, 1, 1, New GregorianCalendar(), DateTimeKind.Utc) ' Compliant
    End Sub
End Class

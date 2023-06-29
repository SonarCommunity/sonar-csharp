﻿Imports System.Globalization

Public Class Program
    Private ReadOnly Epoch As Date = New DateTime(1970, 1, 1) ' Noncompliant {{Use "DateTime.UnixEpoch" instead of creating DateTime instances that point to the unix epoch time}}
    '                                ^^^^^^^^^^^^^^^^^^^^^^^^

    Private ReadOnly EpochOff As DateTimeOffset = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero) ' Noncompliant {{Use "DateTimeOffset.UnixEpoch" instead of creating DateTimeOffset instances that point to the unix epoch time}}
    '                                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    Private Sub BasicCases(ByVal dateTime As Date)
        Dim timeSpan = dateTime - New DateTime(1970, 1, 1) ' Noncompliant

        If dateTime < New DateTime(1970, 1, 1) Then ' Noncompliant
            Return
        End If

        Dim compliant0 = New DateTime(1971, 1, 1) ' Compliant
        Dim compliant1 = New DateTime(1970, 2, 1) ' Compliant
        Dim compliant2 = New DateTime(1970, 1, 2) ' Compliant
        Dim compliant3 = Date.UnixEpoch ' Compliant
        Dim compliant4 = DateTimeOffset.UnixEpoch ' Compliant

        Dim year = 1970
        Dim dateTime2 = New DateTime(year, 1, 1) ' FN
    End Sub

    Private Sub EdgeCases()
        Dim dateTimeOffset = New DateTimeOffset(New DateTime(1970, 1, 1), New TimeSpan(0, 0, 0)) ' Noncompliant
        Dim dateTime = New DateTime(If(True, 1970, 1971), 1, 1) ' FN
        dateTime = New DATETIME(1970, 1, 1) ' Noncompliant
        Dim dateTime2 As Date = New Date(1970, 1, 1) ' Noncompliant
    End Sub

    Private Sub DateTimeConstructors(ByVal ticks As Integer, ByVal year As Integer, ByVal month As Integer, ByVal day As Integer, ByVal hour As Integer, ByVal minute As Integer, ByVal second As Integer, ByVal millisecond As Integer, ByVal calendar As Calendar, ByVal kind As DateTimeKind)
        ' default date
        Dim ctor0_0 = New DateTime() ' Compliant

        ' ticks
        Dim ctor1_0 = New DateTime(1970) ' Compliant
        Dim ctor1_1 = New DateTime(ticks) ' Compliant
        Dim ctor1_2 = New DateTime(ticks:=ticks) ' Compliant

        ' Fixme: new DateTime(UnixEpochTicks, DateTimeKind.Utc)

        ' year, month, and day
        Dim ctor2_0 = New DateTime(1970, 1, 1) ' Noncompliant
        Dim ctor2_1 = New DateTime(year, month, day) ' Compliant
        Dim ctor2_2 = New DateTime(month:=month, day:=day, year:=year) ' Compliant
        Dim ctor2_3 = New DateTime(month:=1, day:=1, year:=1970) ' Noncompliant

        ' year, month, day, and calendar
        Dim ctor3_0 = New DateTime(1970, 1, 1, New GregorianCalendar()) ' Noncompliant
        Dim ctor3_1 = New DateTime(1970, 3, 1, New GregorianCalendar()) ' Compliant
        Dim ctor3_2 = New DateTime(1970, 1, 1, New ChineseLunisolarCalendar()) ' Compliant
        Dim ctor3_3 = New DateTime(month:=1, day:=1, calendar:=New GregorianCalendar(), year:=1970) ' Noncompliant
        Dim ctor3_4 = New DateTime(month:=1, day:=1, calendar:=New ChineseLunisolarCalendar(), year:=1970) ' Compliant

        ' year, month, day, hour, minute, and second
        Dim ctor4_0 = New DateTime(1970, 1, 1, 0, 0, 0) ' Noncompliant
        Dim ctor4_1 = New DateTime(1970, 1, 1, 0, 0, 1) ' Compliant
        Dim ctor4_2 = New DateTime(year:=1970, minute:=minute, month:=1, day:=1, hour:=0, second:=0) ' Compliant
        Dim ctor4_3 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, second:=0) ' Noncompliant

        ' year, month, day, hour, minute, second, and calendar
        Dim ctor5_0 = New DateTime(1970, 1, 1, 0, 0, 0, New GregorianCalendar()) ' Noncompliant
        Dim ctor5_1 = New DateTime(1970, 1, 1, 0, 1, 0, New GregorianCalendar()) ' Compliant
        Dim ctor5_2 = New DateTime(1970, 1, 1, 0, 0, 0, New ChineseLunisolarCalendar()) ' Compliant
        Dim ctor5_3 = New DateTime(year:=1970, second:=0, minute:=0, day:=1, month:=1, hour:=0, calendar:=New GregorianCalendar()) ' Noncompliant
        Dim ctor5_4 = New DateTime(year:=1970, second:=0, minute:=0, day:=1, month:=1, hour:=0, calendar:=calendar) ' Compliant

        ' year, month, day, hour, minute, second, and DateTimeKind value
        Dim ctor6_0 = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) ' Noncompliant
        Dim ctor6_1 = New DateTime(1970, 1, 1, 1, 0, 0, DateTimeKind.Utc) ' Compliant
        Dim ctor6_2 = New DateTime(1970, 1, 1, hour, 0, 0, DateTimeKind.Utc) ' Compliant
        Dim ctor6_3 = New DateTime(month:=1, year:=1970, day:=1, hour:=0, second:=0, minute:=0, kind:=DateTimeKind.Utc) ' Noncompliant
        Dim ctor6_4 = New DateTime(month:=1, year:=1970, day:=1, hour:=hour, second:=0, minute:=0, kind:=DateTimeKind.Utc) ' Compliant
        Dim ctor6_5 = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) ' Compliant
        Dim ctor6_6 = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local) ' Compliant

        ' year, month, day, hour, minute, second, and millisecond
        Dim ctor7_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0) ' Noncompliant
        Dim ctor7_1 = New DateTime(1970, 1, 1, 0, 0, 0, 1) ' Compliant
        Dim ctor7_3 = New DateTime(year, month, day, hour, minute, second, millisecond) ' Compliant
        Dim ctor7_4 = New DateTime(year:=1970, minute:=minute, month:=1, day:=1, hour:=0, millisecond:=0, second:=0) ' Compliant
        Dim ctor7_5 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0) ' Noncompliant

        ' year, month, day, hour, minute, second, millisecond, and calendar
        Dim ctor8_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar()) ' Noncompliant
        Dim ctor8_1 = New DateTime(1970, 1, 1, 0, 0, 0, 1, New GregorianCalendar()) ' Compliant
        Dim ctor8_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, New ChineseLunisolarCalendar()) ' Compliant
        Dim ctor8_3 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, calendar:=New GregorianCalendar()) ' Noncompliant

        ' year, month, day, hour, minute, second, millisecond, and DateTimeKind value
        Dim ctor9_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) ' Noncompliant
        Dim ctor9_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) ' Compliant
        Dim ctor9_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) ' Compliant
        Dim ctor9_3 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, kind:=DateTimeKind.Utc) ' Noncompliant
        Dim ctor9_4 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, kind:=kind) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, calendar and DateTimeKind value
        Dim ctor10_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar(), DateTimeKind.Utc) ' Noncompliant
        Dim ctor10_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar(), DateTimeKind.Local) ' Compliant
        Dim ctor10_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, New ChineseLunisolarCalendar(), DateTimeKind.Utc) ' Compliant
        Dim ctor10_3 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, calendar:=New GregorianCalendar(), millisecond:=0, second:=0, kind:=DateTimeKind.Utc) ' Noncompliant
        Dim ctor10_4 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, calendar:=New GregorianCalendar(), millisecond:=0, second:=second, kind:=DateTimeKind.Utc) ' Compliant

        ' year, month, day, hour, minute, second, millisecond and microsecond
        Dim ctor11_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0) ' Noncompliant
        Dim ctor11_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 1) ' Compliant
        Dim ctor11_2 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, microsecond:=0) ' Noncompliant
        Dim ctor11_3 = New DateTime(year:=1970, microsecond:=0, minute:=minute, month:=1, hour:=0, day:=1, millisecond:=millisecond, second:=0) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, microsecond and calendar
        Dim ctor12_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar()) ' Noncompliant
        Dim ctor12_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 1, New GregorianCalendar()) ' Compliant
        Dim ctor12_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, New ChineseLunisolarCalendar()) ' Compliant
        Dim ctor12_3 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, calendar:=New GregorianCalendar(), millisecond:=0, second:=0, microsecond:=0) ' Noncompliant
        Dim ctor12_4 = New DateTime(year:=1970, minute:=minute, month:=1, day:=1, hour:=0, calendar:=New GregorianCalendar(), millisecond:=0, second:=0, microsecond:=0) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, microsecond and DateTimeKind value
        Dim ctor13_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, DateTimeKind.Utc) ' Noncompliant
        Dim ctor13_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 1, DateTimeKind.Utc) ' Compliant
        Dim ctor13_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, DateTimeKind.Unspecified) ' Compliant
        Dim ctor13_3 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, DateTimeKind.Local) ' Compliant
        Dim ctor13_4 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, kind:=DateTimeKind.Utc, millisecond:=0, second:=0, microsecond:=0) ' Noncompliant
        Dim ctor13_5 = New DateTime(year:=1970, minute:=minute, month:=1, day:=1, hour:=0, kind:=DateTimeKind.Utc, millisecond:=0, second:=0, microsecond:=0) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, microsecond calendar and DateTimeKind value
        Dim ctor14_0 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar(), DateTimeKind.Utc) ' Noncompliant
        Dim ctor14_1 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 1, New GregorianCalendar(), DateTimeKind.Utc) ' Compliant
        Dim ctor14_2 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar(), DateTimeKind.Unspecified) ' Compliant
        Dim ctor14_3 = New DateTime(1970, 1, 1, 0, 0, 0, 0, 0, New ChineseLunisolarCalendar(), DateTimeKind.Utc) ' Compliant
        Dim ctor14_4 = New DateTime(year:=1970, minute:=0, month:=1, day:=1, hour:=0, kind:=DateTimeKind.Utc, millisecond:=0, second:=0, microsecond:=0, calendar:=New GregorianCalendar()) ' Noncompliant
        Dim ctor14_5 = New DateTime(year:=1970, minute:=minute, month:=1, day:=1, hour:=0, kind:=DateTimeKind.Utc, millisecond:=0, second:=0, microsecond:=0, calendar:=calendar) ' Compliant
    End Sub

    Private Sub DateTimeOffsetConstructors(ByVal timeSpan As TimeSpan, ByVal dateTime As Date, ByVal ticks As Integer, ByVal year As Integer, ByVal month As Integer, ByVal day As Integer, ByVal hour As Integer, ByVal minute As Integer, ByVal second As Integer, ByVal millisecond As Integer, ByVal microsecond As Integer, ByVal calendar As Calendar, ByVal kind As DateTimeKind)
        ' default date
        Dim ctor0_0 = New DateTimeOffset() ' Compliant

        ' datetime
        Dim ctor1_0 = New DateTimeOffset(New DateTime()) ' Compliant
        Dim ctor1_1 = New DateTimeOffset(dateTime) ' Compliant
        Dim ctor1_2 = New DateTimeOffset(New DateTime(1970, 1, 1)) ' Noncompliant

        ' datetime and timespan
        Dim ctor2_0 = New DateTimeOffset(New DateTime(), TimeSpan.Zero) ' Compliant
        Dim ctor2_1 = New DateTimeOffset(New DateTime(), timeSpan) ' Compliant
        Dim ctor2_2 = New DateTimeOffset(New DateTime(1970, 1, 1), TimeSpan.Zero) ' Noncompliant

        ' year, month, day, hour, minute, second, millisecond, offset and calendar
        Dim ctor3_0 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, calendar, timeSpan) ' Compliant
        Dim ctor3_1 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar(), TimeSpan.Zero) ' Noncompliant
        Dim ctor3_2 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar(), New TimeSpan(0)) ' Noncompliant
        Dim ctor3_3 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, New GregorianCalendar(), New TimeSpan(1)) ' Compliant
        Dim ctor3_4 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=TimeSpan.Zero) ' Noncompliant
        Dim ctor3_5 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=New TimeSpan(0)) ' Noncompliant
        Dim ctor3_6 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=New TimeSpan(2)) ' Compliant
        Dim ctor3_7 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, millisecond:=0, calendar:=calendar, offset:=New TimeSpan(0)) ' Compliant

        ' year, month, day, hour, minute, second, millisecond, microsecond, offset and calendar
        Dim ctor4_0 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, calendar, timeSpan) ' Compliant
        Dim ctor4_1 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar(), TimeSpan.Zero) ' Noncompliant
        Dim ctor4_2 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar(), New TimeSpan(0)) ' Noncompliant
        Dim ctor4_3 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, New GregorianCalendar(), New TimeSpan(1)) ' Compliant
        Dim ctor4_4 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 1, New GregorianCalendar(), New TimeSpan(0)) ' Compliant
        Dim ctor4_5 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, microsecond:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=TimeSpan.Zero) ' Noncompliant
        Dim ctor4_6 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, microsecond:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=New TimeSpan(0)) ' Noncompliant
        Dim ctor4_7 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, microsecond:=0, millisecond:=0, calendar:=New GregorianCalendar(), offset:=New TimeSpan(2)) ' Compliant
        Dim ctor4_8 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, microsecond:=0, millisecond:=0, calendar:=calendar, offset:=New TimeSpan(0)) ' Compliant
        Dim ctor4_9 = New DateTimeOffset(hour:=0, month:=1, day:=1, year:=1970, minute:=0, second:=0, microsecond:=1, millisecond:=0, calendar:=New GregorianCalendar(), offset:=New TimeSpan(0)) ' Compliant

        ' year, month, day, hour, minute, second and offset
        Dim ctor5_0 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, New TimeSpan(0)) ' Noncompliant
        Dim ctor5_1 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, New TimeSpan(1)) ' Compliant
        Dim ctor5_2 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, timeSpan) ' Compliant
        Dim ctor5_3 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, timeSpan) ' Compliant
        Dim ctor5_4 = New DateTimeOffset(1970, 1, 1, 0, 0, 2, New TimeSpan(0)) ' Compliant
        Dim ctor5_5 = New DateTimeOffset(year:=1970, minute:=0, month:=1, day:=1, hour:=0, second:=0, offset:=New TimeSpan(0)) ' Noncompliant

        ' year, month, day, hour, minute, second, millisecond and offset
        Dim ctor6_0 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, New TimeSpan(0)) ' Noncompliant
        Dim ctor6_1 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero) ' Noncompliant
        Dim ctor6_2 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, New TimeSpan(2, 14, 18)) ' Compliant
        Dim ctor6_3 = New DateTimeOffset(1970, 1, 1, 0, 1, 0, 0, TimeSpan.Zero) ' Compliant
        Dim ctor6_4 = New DateTimeOffset(year:=1970, minute:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, offset:=New TimeSpan(0)) ' Noncompliant

        ' year, month, day, hour, minute, second, millisecond and offset
        Dim ctor7_0 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, New TimeSpan(0)) ' Noncompliant
        Dim ctor7_1 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, New TimeSpan(2, 14, 18)) ' Compliant
        Dim ctor7_2 = New DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero) ' Noncompliant
        Dim ctor7_3 = New DateTimeOffset(1970, 1, 1, 0, 1, 0, 0, 0, TimeSpan.Zero) ' Compliant
        Dim ctor7_4 = New DateTimeOffset(year:=1970, minute:=0, microsecond:=0, month:=1, day:=1, hour:=0, millisecond:=0, second:=0, offset:=New TimeSpan(0)) ' Noncompliant
    End Sub
End Class

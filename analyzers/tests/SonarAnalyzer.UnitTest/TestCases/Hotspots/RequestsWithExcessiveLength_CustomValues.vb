﻿Imports System
Imports Microsoft.AspNetCore.Mvc

Namespace Tests.TestCases

    Public Class MyController
        Inherits Controller

        <HttpPost>
        <RequestSizeLimit(43)>
        Public Function PostRequestAboveLimit() As ActionResult
            Return Nothing
        End Function
        '^^^^^^^^^^^^^^^^^^^^ Noncompliant@-3 {{Make sure the content length limit is safe here.}}

        <HttpPost>
        <RequestFormLimits(MultipartBodyLengthLimit:=43)>
        Public Function MultipartFormRequestAboveLimit() As ActionResult
            Return Nothing
        End Function
        '^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ Noncompliant@-3 {{Make sure the content length limit is safe here.}}

        <HttpPost>
        <RequestSizeLimit(41)>
        Public Function PostRequestBelowLimit() As ActionResult
            Return Nothing
        End Function

        <HttpPost>
        <RequestFormLimits(MultipartBodyLengthLimit:=41)>
        Public Function MultipartFormRequestBelowLimit() As ActionResult
            Return Nothing
        End Function

    End Class

End Namespace

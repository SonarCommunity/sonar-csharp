﻿class Compliant
{
    int SingleLine() { return 42; } // Single line comment.
    int BlockComment() { return 42; } /* Single line comment. */
}

class Noncompliant
{
    // Fixed
    int SingleLine() { return 42; }
}

﻿class Fixes
{
    int SingleLine() { return 42; }

    int SingleLineWithSpacing() { return 42; }

    /*  Multi lines with a mix like
     *  return 42;
     *  are not removed.
     */

    int SeperateLine() { return 42; }

    int WithinLine() { return 42; }

    int MultipleLines() { return 42; }

    int MultipleLinesWithSpace()
    {
        return 42;
    }
}

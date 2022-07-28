﻿using System.IO;

public class DisposableNotDisposed
{
    private struct InnerStruct
    {
        public InnerStruct() { }

        private FileStream inner_field_fs1 = new FileStream(@"c:\foo.txt", FileMode.Open); // Noncompliant - should be reported on once
    }
}

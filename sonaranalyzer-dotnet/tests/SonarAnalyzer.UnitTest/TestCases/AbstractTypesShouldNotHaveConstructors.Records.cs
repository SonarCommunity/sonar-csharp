﻿// See https://github.com/dotnet/roslyn/issues/45510
namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}

namespace Tests.Diagnostics
{
    abstract record AbstractRecordOne
    {
        public string X { get; }

        public AbstractRecordOne(string x) => (X) = (x); // FN
    }

    record RecordOne : AbstractRecordOne
    {
        public RecordOne(string x) : base(x) { } // Compliant
    }

    abstract record AbstractRecordTwo(string Y);

    record RecordTwo(string Z) : AbstractRecordTwo(Z);
}

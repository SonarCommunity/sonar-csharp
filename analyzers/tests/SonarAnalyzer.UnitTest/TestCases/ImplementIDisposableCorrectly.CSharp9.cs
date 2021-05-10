﻿using System;

namespace Tests.Diagnostics
{
    public sealed record SealedDisposable : IDisposable
    {
        public void Dispose() { }
    }

    public record SimpleDisposable : IDisposable // Noncompliant {{Fix this implementation of 'IDisposable' to conform to the dispose pattern.}}
    {
        public void Dispose() => Dispose(true); // Secondary {{'SimpleDisposable.Dispose()' should also call 'GC.SuppressFinalize(this)'.}}

        protected virtual void Dispose(bool disposing) { }
    }

    public record SimpleDisposableWithSuppressFinalize : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public record DisposableWithMoreThanTwoStatements : IDisposable // Noncompliant
    {
        public void Dispose() // Secondary {{'DisposableWithMoreThanTwoStatements.Dispose()' should call 'Dispose(true)', 'GC.SuppressFinalize(this)' and nothing else.}}
        {
            Dispose(true);
            Console.WriteLine("Extra statement");
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public record DerivedDisposable : SimpleDisposable
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public record FinalizedDisposable : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        ~FinalizedDisposable()
        {
            Dispose(false);
        }
    }

    public record FinalizedDisposableExpression : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        ~FinalizedDisposableExpression() =>
            Dispose(false);
    }

    public record NoVirtualDispose : IDisposable
//                ^^^^^^^^^^^^^^^^ Noncompliant {{Fix this implementation of 'IDisposable' to conform to the dispose pattern.}}
//                ^^^^^^^^^^^^^^^^ Secondary@-1 {{Provide 'protected' overridable implementation of 'Dispose(bool)' on 'NoVirtualDispose' or mark the type as 'sealed'.}}
    {
        public void Dispose() { }
//                  ^^^^^^^ Secondary {{'NoVirtualDispose.Dispose()' should call 'Dispose(true)' and 'GC.SuppressFinalize(this)'.}}

        public virtual void Dispose(bool a, bool b) { } // This should not affect the implementation
    }

    public record ExplicitImplementation : IDisposable // Noncompliant
    {
        void IDisposable.Dispose()
//                       ^^^^^^^ Secondary {{'ExplicitImplementation.Dispose()' should also call 'GC.SuppressFinalize(this)'.}}
//                       ^^^^^^^ Secondary@-1 {{'ExplicitImplementation.Dispose()' should be 'public'.}}
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public record VirtualImplementation : IDisposable // Noncompliant
    {
        public virtual void Dispose()
//             ^^^^^^^ Secondary {{'VirtualImplementation.Dispose()' should not be 'virtual' or 'abstract'.}}
//                          ^^^^^^^ Secondary@-1 {{'VirtualImplementation.Dispose()' should also call 'GC.SuppressFinalize(this)'.}}
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }
    }

    public record WithFinalizer : IDisposable // Noncompliant
    {
        public void Dispose()
//                  ^^^^^^^ Secondary {{'WithFinalizer.Dispose()' should also call 'GC.SuppressFinalize(this)'.}}
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        ~WithFinalizer() { }
//       ^^^^^^^^^^^^^ Secondary {{Modify 'WithFinalizer.~WithFinalizer()' so that it calls 'Dispose(false)' and then returns.}}
    }

    public record WithFinalizer2 : IDisposable // Noncompliant
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        ~WithFinalizer2() // Secondary, more than one line
        {
            Dispose(false);
            Console.WriteLine(1);
            Console.WriteLine(2);
        }
    }

    public record DerivedDisposable1 : NoVirtualDispose // Compliant, we are not in charge of our base
    {
    }

    public record DerivedDisposable2 : SimpleDisposable // Compliant, we do not override Dispose(bool)
    {
    }

    public record DisposeNotCallingBase1 : SimpleDisposable // Noncompliant
    {
        protected override void Dispose(bool disposing) { }
//                              ^^^^^^^ Secondary {{Modify 'Dispose(disposing)' so that it calls 'base.Dispose(disposing)'.}}
    }

    public record DisposeNotCallingBase2 : DerivedDisposable2 // Noncompliant, checking for deeper inheritance here
    {
        protected override void Dispose(bool disposing)
//                              ^^^^^^^ Secondary {{Modify 'Dispose(disposing)' so that it calls 'base.Dispose(disposing)'.}}
        {
        }
    }

    public interface IMyDisposable : IDisposable // Compliant, interface
    {
    }

    public record DerivedWithInterface1 : NoVirtualDispose, IDisposable
//                ^^^^^^^^^^^^^^^^^^^^^ Noncompliant
//                                                          ^^^^^^^^^^^ Secondary@-1 {{Remove 'IDisposable' from the list of interfaces implemented by 'DerivedWithInterface1' and override the base class 'Dispose' implementation instead.}}
    {
    }

    public record DerivedWithInterface2 : NoVirtualDispose, IMyDisposable // Compliant, we are not in charge of the interface
    {
    }

    public partial record PartialCompliant : IDisposable // Noncompliant FP, should be Compliant
                                                         // Secondary@-1 FP
    {
        public partial void Dispose();
    }

    public partial record PartialCompliant // Noncompliant FP, should be Compliant
                                           // Secondary@-1 FP
    {
        public partial void Dispose() { }
    }

    public partial record PartialSimpleDisposable : IDisposable // FN, should be Non-compliant with Fix this implementation of 'IDisposable' to conform to the dispose pattern.
    {
        public partial void Dispose();
        protected virtual partial void Dispose(bool disposing);
    }

    public partial record PartialSimpleDisposable // FN, should be Non-compliant with Fix this implementation of 'IDisposable' to conform to the dispose pattern.}}
    {
        public partial void Dispose() => Dispose(true); // second location with, 'SimpleDisposable.Dispose()' should also call 'GC.SuppressFinalize(this)'.

        protected virtual partial void Dispose(bool disposing) { }
    }
}

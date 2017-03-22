using System;

namespace Tests.Diagnostics
{
    public interface IInterface1
    {
        int InterfaceMethod1();
        int InterfaceProperty1 { get; set; }
    }

    public class Class1
    {
        private int instanceMember = 0;
        private static int staticMember = 0;
        protected IInterface1 instanceInterface = null;

        // properties
        public int Property1 { get; }
        public int Property2 { get; set; }
        public int Property3 { get { return instanceMember; } }
        public int Property4 { get { return this.instanceMember; } }
        public int Property5 { get { return staticMember; } } // Noncompliant {{Make 'Property5' a static property.}}
        public int Property6 { get { return Class1.staticMember; } } // Noncompliant
        public int Property7 { get { return new Class1().instanceMember; } } // Noncompliant
        public int Property8 { get { return 0; } } // Noncompliant
        public int Property9 { get { return 0; } set { instanceMember = value; } }
        public int Property10 { get { return 0; } set { this.instanceMember = value; } }
        public int Property11 => 0; // Noncompliant
        public int Property12 => instanceMember;
        public int Property13 => this.instanceMember;
        public int Property14 => staticMember; // Noncompliant
        public int Property15 => Class1.staticMember; // Noncompliant
        public int Property16 => new Class1().instanceMember; // Noncompliant
        public int Property17 => instanceInterface.InterfaceProperty1;
        public Class1 Property18 => this;
        public static int StaticProperty1 => 0;
        public static int StaticProperty2 => staticMember;
        public virtual int VirtualProperty { get; set; }

        // indexers are always instance
        public int this[string index] { get { return 0; } } // Compliant!

        // methods
        public int Method1() { return 0; } // Noncompliant
        public int Method2() { return instanceMember; }
        public int Method3() { return this.instanceMember; }
        public int Method4() { return staticMember; } // Noncompliant
        public int Method5() { return Class1.staticMember; } // Noncompliant {{Make 'Method5' a static method.}}
        public int Method6() { return new Class1().instanceMember; } // Noncompliant
        public int Method7(Class1 arg) { return arg.instanceMember; } // Noncompliant
        public int Method8(Class1 arg) { return arg.instanceInterface.InterfaceProperty1; } // Noncompliant
        public int Method8_0(Class1 arg) { return (arg).instanceInterface.InterfaceProperty1; } // Noncompliant
        public int Method8_1(Class1 arg) { return (int)arg?.instanceInterface?.InterfaceProperty1; } // Noncompliant
        public int Method8_2(Class1 arg) { return (int)((arg))?.instanceInterface?.InterfaceProperty1; } // Noncompliant
        public int Method8_3(Class1 arg) { return ((int)arg)?.instanceInterface?.InterfaceProperty1; } // Noncompliant
        public void Method9() { (Property2 + 1).ToString(); }

        public int Method10() => 0; // Noncompliant
        public int Method11() => instanceMember;
        public int Method12() => this.instanceMember;
        public int Method13() => staticMember; // Noncompliant
        public int Method13_1() => (staticMember); // Noncompliant
        public int Method14() => Class1.staticMember; // Noncompliant
        public int Method14_1() => (Class1).staticMember; // Noncompliant
        public int Method15() => new Class1().instanceMember; // Noncompliant
        public int Method15_1() => (new Class1()).instanceMember; // Noncompliant
        public int Method16() => 0; // Noncompliant
        public int Method17(Class1 arg) => arg.instanceMember; // Noncompliant
        public int Method18(Class1 arg) => arg.instanceInterface.InterfaceProperty1; // Noncompliant
        public int Method19() { return instanceInterface.InterfaceProperty1.GetHashCode(); }
        public int Method19_1() { return ((((((instanceInterface))).InterfaceProperty1))).GetHashCode(); }
        public int Method19_2() { return (((((((((this))).instanceInterface))).InterfaceProperty1))).GetHashCode(); }
        public Class1 Method21() => this;
        public string Method22() => nameof(instanceMember); // Noncompliant

        public static int StaticMethod1() { return 0; }
        public static int StaticMethod2() { return staticMember; }
        public static int StaticMethod3() => 0;
        public static int StaticMethod4() => staticMember;

        public bool Method30() { return 0 > instanceMember; }
        public bool Method31() { var a = instanceMember; return a > 0; }
        public bool Method32() { if (instanceMember - 5 > 10) return true; else return false; }
        public int Method33() { return Math.Abs(instanceMember); }

        public virtual int VirtualMethod() { return 0; }

        protected void GenericMethod<T>(T arg) { /*do nothing*/ }
        protected void Method34() { GenericMethod<int>(5); }
    }

    public abstract class Class2
    {
        public abstract int AbstractProperty { get; set; }
        public abstract int AbstractMethod();
    }

    public class Class3 : Class1
    {
        public override int VirtualProperty { get { return 0; } set { /*do nothing*/ } }
        public override int VirtualMethod() { return 0; }
        public new int Method1() { return 0; }
        public new int Property1 { get { return 0; } }
        public bool Method44(Class1 test) { return test.Property1 == instanceInterface.InterfaceProperty1; }
    }

    public class Class4 : IInterface1
    {
        public int InterfaceProperty1 { get { return 0; } set { } } // if Class4 adds an explicit implementation of this property an issue will be raised here
        public int InterfaceMethod1() => 0;
    }
}

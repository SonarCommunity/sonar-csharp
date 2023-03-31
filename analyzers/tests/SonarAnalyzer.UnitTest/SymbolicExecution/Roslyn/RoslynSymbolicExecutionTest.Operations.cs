﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2023 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Microsoft.CodeAnalysis.Operations;
using SonarAnalyzer.Common;
using SonarAnalyzer.SymbolicExecution.Constraints;
using SonarAnalyzer.SymbolicExecution.Roslyn;
using SonarAnalyzer.UnitTest.TestFramework.SymbolicExecution;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.UnitTest.SymbolicExecution.Roslyn
{
    public partial class RoslynSymbolicExecutionTest
    {
        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromLiteral()
        {
            var validator = SETestContext.CreateCS(@"var a = true; Tag(""a"", a);", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: a = true (Implicit)", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("a", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol()
        {
            var validator = SETestContext.CreateCS(@"bool a = true, b; b = a; Tag(""b"", b);", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromLocalConst()
        {
            var validator = SETestContext.CreateCS(@"const string a = null; var b = a; Tag(""b"", b);").Validator;
            validator.ValidateTag("b", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromClassConst()
        {
            const string code = @"
private const string isNull = null;
private const string isNotNull = ""some text"";
public void Method()
{
    var value = isNull;
    Tag(""IsNull"", value);
    value = isNotNull;
    Tag(""IsNotNull"", value);
}";
            var validator = SETestContext.CreateCSMethod(code).Validator;
            validator.ValidateTag("IsNull", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("IsNotNull", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_Chained()
        {
            var validator = SETestContext.CreateCS(@"bool a = true, b, c; c = b = a; Tag(""c"", c);", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: c = b = a", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: c = b = a", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("c", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToParameter_FromLiteral()
        {
            var validator = SETestContext.CreateCS(@"boolParameter = true; Tag(""boolParameter"", boolParameter);", new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate("SimpleAssignment: boolParameter = true", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.Validate("SimpleAssignment: boolParameter = true", x => x.State[((ISimpleAssignmentOperation)x.Operation.Instance).Target].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("boolParameter", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_CS()
        {
            var setter = new PreProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            var validator = SETestContext.CreateCS(@"var b = boolParameter; Tag(""b"", b);", setter).Validator;
            validator.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void SimpleAssignment_ToLocalVariable_FromTrackedSymbol_VB()
        {
            var setter = new PreProcessTestCheck(OperationKind.ParameterReference, x => x.SetOperationConstraint(DummyConstraint.Dummy));
            var validator = SETestContext.CreateVB(@"Dim B As Boolean = BoolParameter : Tag(""B"", B)", setter).Validator;
            validator.ValidateTag("B", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow(@"Sample.StaticField = 42; Tag(""Target"", Sample.StaticField);", "SimpleAssignment: Sample.StaticField = 42")]
        [DataRow(@"StaticField = 42; Tag(""Target"", StaticField);", "SimpleAssignment: StaticField = 42")]
        [DataRow(@"field = 42; Tag(""Target"", field);", "SimpleAssignment: field = 42")]
        [DataRow(@"this.field = 42; Tag(""Target"", this.field);", "SimpleAssignment: this.field = 42")]
        [DataRow(@"field = 42; var a = field; Tag(""Target"", field);", "SimpleAssignment: a = field (Implicit)")]
        public void SimpleAssignment_Fields(string snippet, string operation)
        {
            var validator = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.Validate(operation, x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("Target", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow(@"Sample.StaticProperty = 42; Tag(""Target"", Sample.StaticProperty);")]
        [DataRow(@"StaticProperty = 42; Tag(""Target"", StaticProperty);")]
        [DataRow(@"var arr = new byte[] { 13 }; arr[0] = 42; Tag(""Target"", arr[0]);")]
        [DataRow(@"var dict = new Dictionary<string, int>(); dict[""key""] = 42; Tag(""Target"", dict[""key""]);")]
        [DataRow(@"var other = new Sample(); other.Property = 42; Tag(""Target"", other.Property);")]
        [DataRow(@"this.Property = 42; Tag(""Target"", this.Property);")]
        [DataRow(@"Property = 42; Tag(""Target"", Property);")]
        [DataRow(@"var other = new Sample(); other.field = 42; Tag(""Target"", other.field);")]
        public void SimpleAssignment_ToUnsupported_FromLiteral(string snippet)
        {
            var validator = SETestContext.CreateCS(snippet, new LiteralDummyTestCheck()).Validator;
            validator.Validate("Literal: 42", x => x.State[x.Operation].HasConstraint(DummyConstraint.Dummy).Should().BeTrue("it's scaffolded"));
            validator.ValidateTag("Target", x => (x?.HasConstraint(DummyConstraint.Dummy) ?? false).Should().BeFalse());
        }

        [TestMethod]
        public void Conversion_ToLocalVariable_FromTrackedSymbol_ExplicitCast()
        {
            var validator = SETestContext.CreateCS(@"int a = 42; byte b = (byte)a; var c = (byte)field; Tag(""b"", b); Tag(""c"", c);", new LiteralDummyTestCheck()).Validator;
            validator.ValidateOrder(
                "LocalReference: a = 42 (Implicit)",
                "Literal: 42",
                "SimpleAssignment: a = 42 (Implicit)",
                "LocalReference: b = (byte)a (Implicit)",
                "LocalReference: a",
                "Conversion: (byte)a",
                "SimpleAssignment: b = (byte)a (Implicit)",
                "LocalReference: c = (byte)field (Implicit)",
                "InstanceReference: field (Implicit)",
                "FieldReference: field",
                "Conversion: (byte)field",
                "SimpleAssignment: c = (byte)field (Implicit)",
                @"Literal: ""b""",
                @"Argument: ""b""",
                "LocalReference: b",
                "Conversion: b (Implicit)",
                "Argument: b",
                @"Invocation: Tag(""b"", b)",
                @"ExpressionStatement: Tag(""b"", b);",
                @"Literal: ""c""",
                @"Argument: ""c""",
                "LocalReference: c",
                "Conversion: c (Implicit)",
                "Argument: c",
                @"Invocation: Tag(""c"", c)",
                @"ExpressionStatement: Tag(""c"", c);");
            validator.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
            validator.ValidateTag("c", x => x.AllConstraints.Should().ContainSingle().Which.Should().Be(ObjectConstraint.NotNull));
        }

        [TestMethod]
        public void Conversion_ToLocalVariable_FromLiteral_ImplicitCast()
        {
            var validator = SETestContext.CreateCS(@"byte b = 42; Tag(""b"", b);", new LiteralDummyTestCheck()).Validator;
            validator.ValidateOrder(
                "LocalReference: b = 42 (Implicit)",
                "Literal: 42",
                "Conversion: 42 (Implicit)",
                "SimpleAssignment: b = 42 (Implicit)",
                @"Literal: ""b""",
                @"Argument: ""b""",
                "LocalReference: b",
                "Conversion: b (Implicit)",
                "Argument: b",
                @"Invocation: Tag(""b"", b)",
                @"ExpressionStatement: Tag(""b"", b);");
            validator.ValidateTag("b", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow("Int16")]
        [DataRow("Int32")]
        [DataRow("Int64")]
        [DataRow("decimal")]
        [DataRow("float")]
        [DataRow("double")]
        public void Conversion_BuiltInConversion_PropagateState(string type)
        {
            var code = $"""
                byte b = 42;
                {type} value = b;
                Tag("Value", value);
                """;
            SETestContext.CreateCS(code, new LiteralDummyTestCheck()).Validator.ValidateTag("Value", x => x.HasConstraint(DummyConstraint.Dummy).Should().BeTrue());
        }

        [TestMethod]
        public void Conversion_CustomOperators_DoNotPropagateState()
        {
            const string code = """
            public void Main()
            {
                var isTrue = true;
                WithImplicit withImplicit = isTrue;
                WithExplicit withExplicit = (WithExplicit)isTrue;
                Tag("WithImplicit", withImplicit);
                Tag("WithExplicit", withExplicit);
            }

            public struct WithImplicit
            {
                public static implicit operator WithImplicit(bool b) => new();
            }
            public struct WithExplicit
            {
                public static explicit operator WithExplicit(bool b) => new();
            }
            """;
            var validator = SETestContext.CreateCSMethod(code, new LiteralDummyTestCheck()).Validator;
            validator.ValidateTag("WithImplicit", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));
            validator.ValidateTag("WithExplicit", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));
        }

#if NET

        [TestMethod]
        public void Conversion_CustomOperators_DoNotPropagateState_Half()
        {
            const string code = """
                byte b = 42;
                Half h = b;
                Tag("Half", h);
                """;
            var validator = SETestContext.CreateCS(code, new LiteralDummyTestCheck()).Validator;
            validator.ValidateTag("Half", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));    // While it would be better to propagate constraints here, Half has custom conversion operators
        }

#endif

        [TestMethod]
        public void Conversion_ToLocalNonNullableValueType_CS()
        {
            var validator = SETestContext.CreateCS("""
                object o = null; // Set ObjectConstraint.Null and Dummy (LiteralDummyTestCheck)
                int convert = System.Convert.ToInt32(o);
                int explicitCast = (int)o;
                object implicitBoxing = explicitCast;
                object asBoxing = explicitCast as object;
                object unboxingBoxing = (object)(int)o;

                Tag("Object", o);
                Tag("Convert", convert);
                Tag("Explicit", explicitCast);
                Tag("ImplicitBoxing", implicitBoxing);
                Tag("AsBoxing", asBoxing);
                Tag("UnboxingBoxing", unboxingBoxing);
                """, new LiteralDummyTestCheck()).Validator;
            validator.ValidateTag("Object", x => x.Should().HaveOnlyConstraints(ObjectConstraint.Null, DummyConstraint.Dummy));
            validator.ValidateTag("Convert", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull));
            validator.ValidateTag("Explicit", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("ImplicitBoxing", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("AsBoxing", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("UnboxingBoxing", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
        }

        [TestMethod]
        public void Conversion_ToLocalNonNullableValueType_VB()
        {
            var validator = SETestContext.CreateVB("""
                Dim o As Object = Nothing ' Set ObjectConstraint.Null and Dummy (LiteralDummyTestCheck)
                Dim iCType As Integer = CType(o, Integer)
                Dim iCInt = CInt(o)
                Dim iDirectCast As Integer = DirectCast(o, Integer)
                Dim iImplicit As Integer = o
                Dim iTryCast As Integer = TryCast(o, Object) ' Two conversions: Implicit and TryCast
                Dim iTryCastBoxing As Object = TryCast(iTryCast, Object)

                Tag("Object", o)
                Tag("CType", iCType)
                Tag("CInt", iCInt)
                Tag("DirectCast", iDirectCast)
                Tag("Implicit", iImplicit)
                Tag("TryCast", iTryCast)
                Tag("TryCastBoxing", iTryCastBoxing)
                """, new LiteralDummyTestCheck()).Validator;
            validator.ValidateTag("Object", x => x.Should().HaveOnlyConstraints(ObjectConstraint.Null, DummyConstraint.Dummy));
            validator.ValidateTag("CType", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("CInt", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("DirectCast", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("Implicit", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("TryCast", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
            validator.ValidateTag("TryCastBoxing", x => x.Should().HaveOnlyConstraints(ObjectConstraint.NotNull, DummyConstraint.Dummy));
        }

        [TestMethod]
        public void Argument_Ref_ResetsConstraints_CS() =>
            SETestContext.CreateCS(@"var b = true; Main(boolParameter, ref b); Tag(""B"", b);", ", ref bool outParam").Validator.ValidateTag("B", x => x.Should().BeNull());

        [TestMethod]
        public void Argument_Out_ResetsConstraints_CS() =>
            SETestContext.CreateCS(@"var b = true; Main(boolParameter, out b); Tag(""B"", b); outParam = false;", ", out bool outParam").Validator.ValidateTag("B", x => x.Should().BeNull());

        [TestMethod]
        public void Argument_ByRef_ResetConstraints_VB() =>
            SETestContext.CreateVB(@"Dim B As Boolean = True : Main(BoolParameter, B) : Tag(""B"", B)", ", ByRef ByRefParam As Boolean").Validator.ValidateTag("B", x => x.Should().BeNull());

        [TestMethod]
        public void Argument_ArgList_DoesNotThrow()
        {
            const string code = @"
public void ArgListMethod(__arglist)
{
    ArgListMethod(__arglist(""""));
}";
            SETestContext.CreateCSMethod(code).Validator.ValidateExitReachCount(1);
        }

        [TestMethod]
        public void FlowCapture_SetsCapture()
        {
            var assertions = 0;
            var collector = new PostProcessTestCheck(x =>
            {
                if (x.Operation.Instance.Kind == OperationKind.FlowCaptureReference)
                {
                    var capture = IFlowCaptureReferenceOperationWrapper.FromOperation(x.Operation.Instance);
                    x.State.ResolveCapture(capture.WrappedOperation).Kind.Should().Be(OperationKind.LocalReference);
                    assertions++;
                }
                return x.State;
            });
            SETestContext.CreateCS("string a = null; a ??= arg;", ", string arg", collector);
            assertions.Should().Be(3);  // Block #3 transitive capture, Block #3 BranchValue, Block #4
        }

        [TestMethod]
        public void AnonymousObjectCreation_SetsNotNull()
        {
            const string code = @"
var anonymous = new { a = 42 };
Tag(""Anonymous"", anonymous);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.AnonymousObjectCreation);
            validator.ValidateTag("Anonymous", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void ArrayCreation_SetsNotNull()
        {
            const string code = @"
var arr1 = new int[] { 42 };
var arr2 = new int[0];
int[] arr3 = { };
int[,] arrMulti = new int[2, 3];
int[][] arrJagged = new int[2][];

Tag(""Arr1"", arr1);
Tag(""Arr2"", arr2);
Tag(""Arr3"", arr3);
Tag(""ArrMulti"", arrMulti);
Tag(""ArrJagged"", arrJagged);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.ArrayCreation);
            validator.ValidateTag("Arr1", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Arr2", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Arr3", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("ArrMulti", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("ArrJagged", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void DelegateCreation_SetsNotNull()
        {
            const string code = @"
var pointer = Main; // Delegate creation to encapsulating method
var lambda = () => { };
var del = delegate() {};
Tag(""Pointer"", pointer);
Tag(""Lambda"", lambda);
Tag(""Delegate"", del);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.DelegateCreation);
            validator.ValidateTag("Pointer", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Lambda", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Delegate", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void DynamicObjectCreation_SetsNotNull()
        {
            const string code = @"
var s = new Sample(dynamicArg);
Tag(""S"", s);";
            var validator = SETestContext.CreateCS(code, ", dynamic dynamicArg").Validator;
            validator.ValidateContainsOperation(OperationKind.DynamicObjectCreation);
            validator.ValidateTag("S", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void ObjectCreation_SetsNotNull()
        {
            const string code = @"
object assigned;
var obj = new Object();
var valueType = new Guid();
var declared = new Exception();
assigned = new EventArgs();

Tag(""Declared"", declared);
Tag(""Assigned"", assigned);
Tag(""ValueType"", valueType);
Tag(""Object"", obj);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.ObjectCreation);
            validator.ValidateTag("Declared", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Assigned", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("ValueType", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());   // This is questionable, value types should not have ObjectConstraint
            validator.ValidateTag("Object", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void TypeParameterObjectCreation_SetsNotNull()
        {
            const string code = @"
public void Main<T>() where T : new()
{
    var value = new T();
    Tag(""Value"", value);
}";
            var validator = SETestContext.CreateCSMethod(code).Validator;
            validator.ValidateContainsOperation(OperationKind.TypeParameterObjectCreation);
            validator.ValidateTag("Value", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

#if NET

        [TestMethod]
        public void Index_SetsNotNull()
        {
            const string code = """
                var index = ^0;
                Tag("Index", index);
                """;
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Unary);
            validator.ValidateTag("Index", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));
        }

        [TestMethod]
        public void Range_SetsNotNull()
        {
            const string code = """
                var range = 0..1;
                Tag("Range", range);
                """;
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Range);
            validator.ValidateTag("Range", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));
        }

#endif

        [TestMethod]
        public void Literal_NullAndDefault_SetsNull_CS()
        {
            const string code = @"
Tag(""BeforeObjNull"", argObjNull);
Tag(""BeforeObjDefault"", argObjDefault);
Tag(""BeforeInt"", argInt);
Tag(""BeforeNullableInt"", argNullableInt);
argObjNull = null;
argObjDefault = default;
argInt = default;
argNullableInt = default;
Tag(""AfterObjNull"", argObjNull);
Tag(""AfterObjDefault"", argObjDefault);
Tag(""AfterInt"", argInt);
Tag(""AfterNullableInt"", argNullableInt);";
            var validator = SETestContext.CreateCS(code, ", object argObjNull, object argObjDefault, int argInt, int? argNullableInt").Validator;
            validator.ValidateContainsOperation(OperationKind.Literal);
            validator.ValidateTag("BeforeObjNull", x => x.Should().BeNull());
            validator.ValidateTag("BeforeObjDefault", x => x.Should().BeNull());
            validator.ValidateTag("BeforeInt", x => x.Should().BeNull());
            validator.ValidateTag("BeforeNullableInt", x => x.Should().BeNull());
            validator.ValidateTag("AfterObjNull", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("AfterObjDefault", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("AfterInt", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterNullableInt", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
        }

        [TestMethod]
        public void Literal_Null_SetsNull_VB()
        {
            const string code = @"
Tag(""BeforeObj"", ArgObj)
Tag(""BeforeInt"", ArgInt)
ArgObj = Nothing
ArgInt = Nothing
Tag(""AfterObj"", ArgObj)
Tag(""AfterInt"", ArgInt)";
            var validator = SETestContext.CreateVB(code, ", ArgObj As Object, ArgInt As Integer").Validator;
            validator.ValidateContainsOperation(OperationKind.Literal);
            validator.ValidateTag("BeforeObj", x => x.Should().BeNull());
            validator.ValidateTag("BeforeInt", x => x.Should().BeNull());
            validator.ValidateTag("AfterObj", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("AfterInt", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void Literal_Default_ForGenericType()
        {
            const string code = @"
public void Main<TClass, TStruct, TUnknown, TType, TInterface, TUnmanaged, TEnum, TDelegate>
    (TClass argClass, TStruct argStruct, TUnknown argUnknown, TType argType, TInterface argInterface, TUnmanaged argUnmanaged, TEnum argEnum, TDelegate argDelegate)
    where TClass : class
    where TStruct: struct
    where TType : EventArgs
    where TInterface : IDisposable
    where TUnmanaged : unmanaged
    where TEnum : Enum
    where TDelegate : Delegate
{
    argClass = default;
    argStruct = default;
    argUnknown = default;
    argType = default;
    argInterface = default;
    argUnmanaged = default;
    argEnum = default;
    argDelegate = default;
    Tag(""Class"", argClass);
    Tag(""Struct"", argStruct);
    Tag(""Unknown"", argUnknown);
    Tag(""Type"", argType);
    Tag(""Interface"", argInterface);
    Tag(""Unmanaged"", argUnmanaged);
    Tag(""Enum"", argEnum);
    Tag(""Delegate"", argDelegate);
}";
            var validator = SETestContext.CreateCSMethod(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Literal);
            validator.ValidateTag("Class", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("Struct", x => x.Should().BeNull("struct cannot be null."));
            validator.ValidateTag("Unknown", x => x.Should().BeNull("it can be struct."));
            validator.ValidateTag("Type", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("Interface", x => x.Should().BeNull("interfaces can be implemented by a struct."));
            validator.ValidateTag("Unmanaged", x => x.Should().BeNull("unmanaged implies struct and cannot be null."));
            validator.ValidateTag("Enum", x => x.Should().BeNull("Enum cannot be null."));
            validator.ValidateTag("Delegate", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
        }

        [TestMethod]
        public void Literal_Default_ConversionsFromAnotherType()
        {
            const string code = @"
object o = default(Exception);
int i = default(byte);
Tag(""ObjectFromException"", o);
Tag(""IntegerFromByte"", i);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateTag("ObjectFromException", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("IntegerFromByte", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void Literal_Default_StringLiteral()
        {
            const string code = @"
var stringLocal = ""someText"";
const string stringConst = ""someText"";
Tag(""StringLocal"", stringLocal);
Tag(""StringConst"", stringConst);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateTag("StringLocal", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("StringConst", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow("true")]
        [DataRow("false")]
        public void Literal_Default_BoolLiterals(string literal)
        {
            var expected = bool.Parse(literal);
            var code = @$"
var value = {literal};
Tag(""Value"", value);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateTag("Value", x => x.HasConstraint(BoolConstraint.From(expected)));
        }

        [DataTestMethod]
        [DataRow("'c'")]
        [DataRow("1")]
        [DataRow("1u")]
        [DataRow("1l")]
        [DataRow("0xFF")]
        [DataRow("1.0")]
        [DataRow("1.0f")]
        public void Literal_Default_OtherLiterals(string literal)
        {
            var code = @$"
var value = {literal};
Tag(""Value"", value);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateTag("Value", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void Literal_Default_InferredFromReturnType()
        {
            var validator = SETestContext.CreateCSMethod("public object Main() => default;").Validator;
            validator.ValidateContainsOperation(OperationKind.DefaultValue);    // And do not fail
        }

        [TestMethod]
        public void InstanceReference_SetsNotNull_CS()
        {
            const string code = @"
var fromThis = this;
var _ = field;
Tag(""This"", fromThis);";
            var implicitCheck = new PostProcessTestCheck(OperationKind.FieldReference, x =>
            {
                var reference = (IFieldReferenceOperation)x.Operation.Instance;
                reference.Instance.Kind.Should().Be(OperationKind.InstanceReference);
                reference.Instance.IsImplicit.Should().BeTrue();
                x.State[reference.Instance].HasConstraint(ObjectConstraint.NotNull).Should().BeTrue();
                return x.State;
            });
            var validator = SETestContext.CreateCS(code, implicitCheck).Validator;
            validator.ValidateContainsOperation(OperationKind.InstanceReference);
            validator.ValidateContainsOperation(OperationKind.FieldReference);  // To execute implicitCheck
            validator.ValidateTag("This", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void SizeOf_SetNotNullconstraint()
        {
            var code = """
                var size = sizeof(int);
                Tag("Size", size);
                """;
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateTag("Size", x => x.Should().HaveOnlyConstraint(ObjectConstraint.NotNull));
        }

        [DataTestMethod]
        [DataRow("[ValidatedNotNull]")]
        [DataRow("[ValidatedNotNullAttribute]")]
        [DataRow("[NotNull]")]
        [DataRow("[NotNullAttribute]")]
        public void Invocation_NotNullAttribute_SetsNotNullOnArgumentsMarkedWithAttribute(string notNullAttribute)
        {
            var code = $@"
using System;

public class Sample
{{
    public void Main(object o1, object o2, object o3, object o4, object o5, object o6, object o7, object o8, string s1, string s2)
    {{
        Guard.NotNullExt(o1);
        o2.NotNullExt();
        NotNullInst(o3);
        NotNullInst(o4, o5);
        NotNullInst(value2: o6, value1: o7, value3: o8);  // value2 is not annotated
        NotNullRefOut(ref s1, out s2);
        Tag(""AfterGuard_o1"", o1);
        Tag(""AfterGuard_o2"", o2);
        Tag(""AfterGuard_o3"", o3);
        Tag(""AfterGuard_o4"", o4);
        Tag(""AfterGuard_o5"", o5);
        Tag(""AfterGuard_o6"", o6);
        Tag(""AfterGuard_o7"", o7);
        Tag(""AfterGuard_o8"", o8);
        Tag(""AfterGuard_s1"", s1);
        Tag(""AfterGuard_s2"", s2);
    }}

    private void NotNullInst({notNullAttribute} object value)
    {{
        // Skip implementation to make sure, the attribute is driving the constraint
    }}

    private void NotNullInst({notNullAttribute} object value1, {notNullAttribute} object value2) {{ }}
    private void NotNullInst<T1, T2, T3>({notNullAttribute} T1 value1, T2 value2, {notNullAttribute} T3 value3) {{ }}
    private void NotNullRefOut<T1, T2>({notNullAttribute} ref T1 value1, {notNullAttribute} out T2 value2) {{ value1 = default; value2 = default; }}

    private static void Tag(string name, object arg) {{ }}
}}

public sealed class ValidatedNotNullAttribute : Attribute {{ }}

public sealed class NotNullAttribute : Attribute {{ }}

public static class Guard
{{
    public static void NotNullExt<T>({notNullAttribute} this T value) where T : class {{ }}
}}
";
            var validator = new SETestContext(code, AnalyzerLanguage.CSharp, Array.Empty<SymbolicCheck>()).Validator;
            validator.ValidateTag("AfterGuard_o1", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o2", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o3", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o4", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o5", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o6", x => x.Should().BeNull()); // parameter is not annotated
            validator.ValidateTag("AfterGuard_o7", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_o8", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_s1", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterGuard_s2", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void Invocation_Read_SetsNotNull()
        {
            const string code = @"
_ = StaticField;            // Do not fail, do nothing
_ = Sample.StaticField;
_ = field;
_ = UntrackedSymbol().field;
Tag(""Before"", arg);
_ = arg.field;
Tag(""After"", arg);

Sample UntrackedSymbol() => this;";
            var validator = SETestContext.CreateCS(code, ", Sample arg").Validator;
            validator.ValidateContainsOperation(OperationKind.FieldReference);
            validator.ValidateTag("Before", x => x.Should().BeNull());
            validator.ValidateTag("After", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void FieldReference_Write_SetsNotNull()
        {
            const string code = @"
StaticField = 42;            // Do not fail, do nothing
Sample.StaticField = 42;
field = 42;
UntrackedSymbol().field = 42;
Tag(""Before"", arg);
arg.field = 42;
Tag(""After"", arg);

Sample UntrackedSymbol() => this;";
            var validator = SETestContext.CreateCS(code, ", Sample arg").Validator;
            validator.ValidateContainsOperation(OperationKind.FieldReference);
            validator.ValidateTag("Before", x => x.Should().BeNull());
            validator.ValidateTag("After", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void FieldReference_DoesNotMixInstances()
        {
            const string code = @"
this.fieldException = new NotImplementedException();
var argException = arg.fieldException;  // Should not propagate constraint from this.fieldException
Tag(""This"", fieldException);
Tag(""Arg"", argException);";
            var validator = SETestContext.CreateCS(code, ", Sample arg").Validator;
            validator.ValidateTag("This", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("Arg", x => x.Should().BeNull());
        }

        [TestMethod]
        public void PropertyReference_Read_SetsNotNull()
        {
            const string code = @"
_ = StaticProperty;            // Do not fail, do nothing
_ = Sample.StaticProperty;
_ = Property;
_ = UntrackedSymbol().Property;
Tag(""BeforeProperty"", arg);
Tag(""BeforeDictionary"", dictionary);
Tag(""BeforeIndexer"", indexer);
_ = arg.Property;
_ = dictionary[42];
_ = indexer[42];
Tag(""AfterProperty"", arg);
Tag(""AfterDictionary"", dictionary);
Tag(""AfterIndexer"", indexer);

Sample UntrackedSymbol() => this;";
            var validator = SETestContext.CreateCS(code, ", Sample arg, Dictionary<int, int> dictionary, Sample indexer").Validator;
            validator.ValidateContainsOperation(OperationKind.PropertyReference);
            validator.ValidateTag("BeforeProperty", x => x.Should().BeNull());
            validator.ValidateTag("BeforeDictionary", x => x.Should().BeNull());
            validator.ValidateTag("BeforeIndexer", x => x.Should().BeNull());
            validator.ValidateTag("AfterProperty", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterDictionary", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterIndexer", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void PropertyReference_Write_SetsNotNull()
        {
            const string code = @"
StaticProperty = 42;            // Do not fail, do nothing
Sample.StaticProperty = 42;
Property = 42;
UntrackedSymbol().Property = 42;
Tag(""BeforeProperty"", arg);
Tag(""BeforeDictionary"", dictionary);
Tag(""BeforeIndexer"", indexer);
arg.Property = 42;
dictionary[42] = 42;
indexer[42] = 42;
Tag(""AfterProperty"", arg);
Tag(""AfterDictionary"", dictionary);
Tag(""AfterIndexer"", indexer);

Sample UntrackedSymbol() => this;";
            var validator = SETestContext.CreateCS(code, ", Sample arg, Dictionary<int, int> dictionary, Sample indexer").Validator;
            validator.ValidateContainsOperation(OperationKind.PropertyReference);
            validator.ValidateTag("BeforeProperty", x => x.Should().BeNull());
            validator.ValidateTag("BeforeDictionary", x => x.Should().BeNull());
            validator.ValidateTag("BeforeIndexer", x => x.Should().BeNull());
            validator.ValidateTag("AfterProperty", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterDictionary", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterIndexer", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void ArrayElementReference_Read_SetsNotNull()
        {
            const string code = @"
_ = UntrackedSymbol()[42]; // Do not fail
Tag(""Before"", array);
_ = array[42];
Tag(""After"", array);

int[] UntrackedSymbol() => new[] { 42 };";
            var validator = SETestContext.CreateCS(code, ", int[] array").Validator;
            validator.ValidateContainsOperation(OperationKind.ArrayElementReference);
            validator.ValidateTag("Before", x => x.Should().BeNull());
            validator.ValidateTag("After", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void ArrayElementReference_Write_SetsNotNull()
        {
            const string code = @"
UntrackedSymbol()[42] = 42; // Do not fail
Tag(""Before"", array);
array[42] = 42;
Tag(""After"", array);

int[] UntrackedSymbol() => new[] { 42 };";
            var validator = SETestContext.CreateCS(code, ", int[] array").Validator;
            validator.ValidateContainsOperation(OperationKind.ArrayElementReference);
            validator.ValidateTag("Before", x => x.Should().BeNull());
            validator.ValidateTag("After", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void EventReference_SetsNotNull()
        {
            const string code = @"
Tag(""BeforeAdd"", add);
Tag(""BeforeRemove"", remove);
add.Event += (sender, e) => { };
remove.Event -= (sender, e) => { };
Tag(""AfterAdd"", add);
Tag(""AfterRemove"", remove);";
            var validator = SETestContext.CreateCS(code, ", Sample add, Sample remove").Validator;
            validator.ValidateContainsOperation(OperationKind.ArrayElementReference);
            validator.ValidateTag("BeforeAdd", x => x.Should().BeNull());
            validator.ValidateTag("BeforeRemove", x => x.Should().BeNull());
            validator.ValidateTag("AfterAdd", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterRemove", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void ReDim_SetsNotNull()
        {
            const string code = @"
Dim First(), Second(), Third(4242) As Object
Dim Fourth As Object()
Tag(""BeforeFirst"", First)
Tag(""BeforeSecond"", Second)
Tag(""BeforeThird"", Third)
Tag(""BeforeFourth"", Fourth)
ReDim First(42), Second(1042), Third(4444), Fourth(4), Arg.FieldArray(42)
Tag(""AfterFirst"", First)
Tag(""AfterSecond"", Second)
Tag(""AfterThird"", Third)
Tag(""AfterFourth"", Fourth)
Tag(""AfterNotTracked"", Arg.FieldArray)";
            var validator = SETestContext.CreateVB(code, ", Arg As Sample").Validator;
            validator.ValidateContainsOperation(OperationKind.ReDim);
            validator.ValidateTag("BeforeFirst", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("BeforeSecond", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("BeforeThird", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue("has size in declaration"));
            validator.ValidateTag("BeforeFourth", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("AfterFirst", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterSecond", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterThird", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterFourth", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterNotTracked", x => x.Should().BeNull());
        }

        [TestMethod]
        public void ReDimPreserve_SetsNotNull()
        {
            const string code = @"
Dim First(), Second(10) As Object
Tag(""BeforeFirst"", First)
Tag(""BeforeSecond"", Second)
ReDim Preserve First(42), Second(42)
Tag(""AfterFirst"", First)
Tag(""AfterSecond"", Second)";
            var validator = SETestContext.CreateVB(code, ", Arg As Sample").Validator;
            validator.ValidateContainsOperation(OperationKind.ReDim);
            validator.ValidateTag("BeforeFirst", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("BeforeSecond", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue("has size in declaration"));
            validator.ValidateTag("AfterFirst", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
            validator.ValidateTag("AfterSecond", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [TestMethod]
        public void Await_ForgetsFieldStates()
        {
            const string code = @"
private object field;

public async System.Threading.Tasks.Task Main(System.Threading.Tasks.Task T)
{
    field = null;
    Tag(""Before"", field);
    await T;
    Tag(""After"", field);
}";
            var addConstraint = new PostProcessTestCheck(OperationKind.Literal, x => x.SetOperationConstraint(LockConstraint.Held));    // Persisted constraint
            var validator = SETestContext.CreateCSMethod(code, addConstraint).Validator;
            validator.ValidateTag("Before", x => x.HasConstraint(ObjectConstraint.Null).Should().BeTrue());
            validator.ValidateTag("Before", x => x.HasConstraint(LockConstraint.Held).Should().BeTrue());
            validator.ValidateTag("After", x => x.HasConstraint(ObjectConstraint.Null).Should().BeFalse());
            validator.ValidateTag("After", x => x.HasConstraint(LockConstraint.Held).Should().BeTrue("this constraint should be preserved on fields"));
        }

        [TestMethod]
        public void TypeOf_SetsNotNull()
        {
            var validator = SETestContext.CreateCS(@"var value = typeof(object); Tag(""Value"", value);").Validator;
            validator.ValidateContainsOperation(OperationKind.TypeOf);
            validator.ValidateTag("Value", x => x.HasConstraint(ObjectConstraint.NotNull).Should().BeTrue());
        }

        [DataTestMethod]
        [DataRow("bool", "true", ConstraintKind.ObjectNotNull, ConstraintKind.BoolFalse)]
        [DataRow("bool", "false", ConstraintKind.ObjectNotNull, ConstraintKind.BoolTrue)]
        [DataRow("bool?", "default", ConstraintKind.ObjectNull)]
        [DataRow("bool?", "null", ConstraintKind.ObjectNull)]
        public void Unary_Not_SupportsBoolAndNull(string type, string defaultValue, params ConstraintKind[] expectedConstraints)
        {
            var code = $@"
{type} value = {defaultValue};
value = !value;
Tag(""Value"", value);";
            var validator = SETestContext.CreateCS(code).Validator;
            validator.ValidateContainsOperation(OperationKind.Unary);
            validator.ValidateTag("Value", x => x.AllConstraints.Select(y => y.Kind).Should().BeEquivalentTo(expectedConstraints));
        }

        [TestMethod]
        public void ParameterReference_NonNullableValue_DoesNotPropagateState()
        {
            const string code = """
                public class WithValue
                {
                    public object Value {get;}
                };

                public void Main()
                {
                    var instance = new WithValue();
                    var value = instance.Value;     // Same name as Nullable.Value
                    Tag("Value", value);
                }
                """;
            SETestContext.CreateCSMethod(code).Validator.ValidateTag("Value", x => x.Should().BeNull());
        }
    }
}

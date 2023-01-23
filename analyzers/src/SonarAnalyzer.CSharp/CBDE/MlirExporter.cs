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

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SonarAnalyzer.CFG.Sonar;

namespace SonarAnalyzer.CBDE
{
    public class MlirExporter
    {
        internal static readonly ImmutableList<SyntaxKind> unsupportedSyntaxes = new List<SyntaxKind>
        {
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.AwaitExpression,
            SyntaxKind.CheckedExpression,
            SyntaxKind.CheckedStatement,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.ConditionalExpression,
            SyntaxKind.ConditionalAccessExpression,
            SyntaxKind.FixedStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.GotoStatement,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.SwitchStatement,
            SyntaxKind.TryStatement,
            SyntaxKind.UncheckedExpression,
            SyntaxKind.UncheckedStatement,
            SyntaxKind.UsingStatement,
            SyntaxKind.YieldReturnStatement,
            SyntaxKind.YieldBreakStatement,
            SyntaxKind.UnsafeStatement,
            // The following syntaxes are listed in the same order as SyntaxKindEx, which in turn are in the same order as on StyleCop
            SyntaxKindEx.DotDotToken,
            SyntaxKindEx.QuestionQuestionEqualsToken,
            SyntaxKindEx.NullableKeyword,
            SyntaxKindEx.EnableKeyword,
            SyntaxKindEx.WarningsKeyword,
            SyntaxKindEx.AnnotationsKeyword,
            SyntaxKindEx.VarKeyword,
            SyntaxKindEx.UnderscoreToken,
            SyntaxKindEx.ConflictMarkerTrivia,
            SyntaxKindEx.IsPatternExpression,
            SyntaxKindEx.RangeExpression,
            SyntaxKindEx.CoalesceAssignmentExpression,
            SyntaxKindEx.IndexExpression,
            SyntaxKindEx.DefaultLiteralExpression,
            SyntaxKindEx.LocalFunctionStatement,
            SyntaxKindEx.TupleType,
            SyntaxKindEx.TupleElement,
            SyntaxKindEx.TupleExpression,
            SyntaxKindEx.SingleVariableDesignation,
            SyntaxKindEx.ParenthesizedVariableDesignation,
            SyntaxKindEx.ForEachVariableStatement,
            SyntaxKindEx.DeclarationPattern,
            SyntaxKindEx.ConstantPattern,
            SyntaxKindEx.CasePatternSwitchLabel,
            SyntaxKindEx.WhenClause,
            SyntaxKindEx.DiscardDesignation,
            SyntaxKindEx.RecursivePattern,
            SyntaxKindEx.PropertyPatternClause,
            SyntaxKindEx.Subpattern,
            SyntaxKindEx.PositionalPatternClause,
            SyntaxKindEx.DiscardPattern,
            SyntaxKindEx.SwitchExpression,
            SyntaxKindEx.SwitchExpressionArm,
            SyntaxKindEx.VarPattern,
            SyntaxKindEx.DeclarationExpression,
            SyntaxKindEx.RefExpression,
            SyntaxKindEx.RefType,
            SyntaxKindEx.ThrowExpression,
            SyntaxKindEx.ImplicitStackAllocArrayCreationExpression,
            SyntaxKindEx.SuppressNullableWarningExpression,
            SyntaxKindEx.NullableDirectiveTrivia,
    }.ToImmutableList();

        private readonly TextWriter writer;
        private readonly SemanticModel semanticModel;
        private readonly bool exportsLocations;
        private readonly Dictionary<Block, int> blockMap = new Dictionary<Block, int>();
        private int blockCounter;
        private readonly Dictionary<SyntaxNode, int> opMap = new Dictionary<SyntaxNode, int>();
        private int opCounter;
        private readonly Encoding encoder = Encoding.GetEncoding("ASCII", new PreservingEncodingFallback(), DecoderFallback.ExceptionFallback);
        private readonly MlirExporterMetrics exporterMetrics;

        public MlirExporter(TextWriter w, SemanticModel model, MlirExporterMetrics metrics, bool withLoc)
        {
            writer = w;
            semanticModel = model;
            exportsLocations = withLoc;
            exporterMetrics = metrics;
        }

        public void ExportFunction(MethodDeclarationSyntax method)
        {
            if (method.Body == null)
            {
                return;
            }

            if (IsTooComplexForMlirOrTheCfg(method))
            {
                writer.WriteLine($"// Skipping function {method.Identifier.ValueText}{GetAnonymousArgumentsString(method)}, it contains poisonous unsupported syntaxes");
                writer.WriteLine();
                return;
            }

            if (!CSharpControlFlowGraph.TryGet(method.Body, semanticModel, out var cfg))
            {
                writer.WriteLine($"// Skipping function {method.Identifier.ValueText}{GetAnonymousArgumentsString(method)}, failed to generate CFG");
                writer.WriteLine();
                return;
            }
            blockCounter = 0;
            blockMap.Clear();
            opCounter = 0;
            opMap.Clear();

            var returnType = HasNoReturn(method) ? "()" : MlirType(method.ReturnType);
            writer.WriteLine($"func @{GetMangling(method)}{GetAnonymousArgumentsString(method)} -> {returnType} {GetLocation(method)} {{");
            CreateEntryBlock(method);

            foreach (var block in cfg.Blocks)
            {
                ExportBlock(block, method, returnType);
            }
            writer.WriteLine("}");
        }

        private string GetMangling(BaseMethodDeclarationSyntax method)
        {
            var prettyName = EncodeName(semanticModel.GetDeclaredSymbol(method).ToDisplayString());
            var sb = new StringBuilder(prettyName.Length);
            foreach (var c in prettyName)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
                else if (char.IsSeparator(c))
                {
                    // Ignore it
                }
                else if (c == ',')
                {
                    sb.Append('.');
                }
                else
                {
                    sb.Append('$');
                }
            }
            return sb.ToString();
        }

        private bool IsTooComplexForMlirOrTheCfg(BaseMethodDeclarationSyntax method)
        {
            var symbol = semanticModel.GetDeclaredSymbol(method);
            if (symbol.IsAsync)
            {
                exporterMetrics.AddUnsupportedFunction(SyntaxKind.AsyncKeyword);
                return true;
            }
            foreach (var node in method.DescendantNodes())
            {
                if (unsupportedSyntaxes.Contains(node.Kind()))
                {
                    exporterMetrics.AddUnsupportedFunction(node.Kind());
                    return true;
                }
            }
            exporterMetrics.AddSupportedFunction();
            return false;
        }

        private void CreateEntryBlock(BaseMethodDeclarationSyntax method)
        {
            writer.WriteLine($"^entry {GetArgumentsString(method)}:");
            foreach (var param in method.ParameterList.Parameters)
            {
                if (string.IsNullOrEmpty(param.Identifier.ValueText))
                {
                    // An unnamed parameter cannot be used inside the function
                    continue;
                }
                var id = OpId(param);
                writer.WriteLine($"%{id} = cbde.alloca {MlirType(param)} {GetLocation(param)}");
                writer.WriteLine($"cbde.store %{EncodeName(param.Identifier.ValueText)}, %{id} : memref<{MlirType(param)}> {GetLocation(param)}");
            }
            writer.WriteLine("br ^0");
            writer.WriteLine();
        }

        private bool HasNoReturn(MethodDeclarationSyntax method) =>
            semanticModel.GetTypeInfo(method.ReturnType).Type.SpecialType == SpecialType.System_Void;

        private void ExportReturnStatement(ReturnStatementSyntax ret, string functionReturnType)
        {
            if (ret.Expression == null)
            {
                writer.WriteLine($"return {GetLocation(ret)}");
            }
            else
            {
                Debug.Assert(functionReturnType != "()", "Returning value in function declared with no return type");
                var returnedVal = ret.Expression.RemoveParentheses();
                if (semanticModel.GetTypeInfo(returnedVal).Type == null &&
                    returnedVal.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    // Special case a returning a method group that will be cast into a func
                    writer.WriteLine($"%{OpId(ret)} = cbde.unknown : none {GetLocation(ret)} // return method group");
                    writer.WriteLine($"return %{OpId(ret)} : none {GetLocation(ret)}");
                    return;
                }

                var id = ComputeCompatibleId(ret.Expression, functionReturnType);
                writer.WriteLine($"return %{id} : {functionReturnType} {GetLocation(ret)}");
            }
        }

        private void ExportJumpBlock(JumpBlock jb, string functionReturnType)
        {
            switch (jb.JumpNode)
            {
                case ReturnStatementSyntax ret:
                    ExportReturnStatement(ret, functionReturnType);
                    break;
                case BreakStatementSyntax breakStmt:
                    writer.WriteLine($"br ^{BlockId(jb.SuccessorBlock)} {GetLocation(breakStmt)} // break");
                    break;
                case ContinueStatementSyntax continueStmt:
                    writer.WriteLine($"br ^{BlockId(jb.SuccessorBlock)} {GetLocation(continueStmt)} // continue");
                    break;
                case ThrowStatementSyntax throwStmt:
                    // Should we transfer to a catch block if we are inside a try/catch?
                    // See: https://github.com/SonarSource/SonarCBDE/issues/111
                    writer.WriteLine($"cbde.throw %{OpId(throwStmt.Expression)} :  {MlirType(throwStmt.Expression)} {GetLocation(throwStmt)}");
                    break;
                default:
                    Debug.Assert(false, "Unknown kind of JumpBlock");
                    break;
            }
        }

        private void ExportBinaryBranchBlock(BinaryBranchBlock bbb)
        {
            /*
                     * Currently, we do exactly the same for all cases that may have created a BinaryBranchBlock
                     * (this block can be created for ConditionalExpression, IfStatement, ForEachStatement,
                     * CoalesceExpression, ConditionalAccessExpression, LogicalAndExpression, LogicalOrExpression,
                     * ForStatement and CatchFilterClause) maybe later, we'll do something different depending on
                     * the control structure
                     */
            var cond = GetCondition(bbb);
            if (null == cond)
            {
                Debug.Assert(bbb.BranchingNode.Kind() == SyntaxKind.ForStatement);
                writer.WriteLine($"br ^{BlockId(bbb.TrueSuccessorBlock)}");
            }
            else
            {
                var id = EnforceBoolOpId(cond as ExpressionSyntax);
                writer.WriteLine($"cond_br %{id}, ^{BlockId(bbb.TrueSuccessorBlock)}, ^{BlockId(bbb.FalseSuccessorBlock)} {GetLocation(cond)}");
            }
        }

        private void ExportBlock(Block block, MethodDeclarationSyntax parentMethod, string functionReturnType)
        {
            writer.WriteLine($"^{BlockId(block)}: // {block.GetType().Name}");
            // MLIR encodes blocks relationships in operations, not in blocks themselves
            foreach (var op in block.Instructions)
            {
                ExtractInstruction(op);
            }
            // MLIR encodes blocks relationships in operations, not in blocks themselves
            // So we need to add the corresponding operations at the end...
            switch (block)
            {
                case JumpBlock jb:
                    ExportJumpBlock(jb, functionReturnType);
                    break;
                case BinaryBranchBlock bbb:
                    ExportBinaryBranchBlock(bbb);
                    break;
                case SimpleBlock sb:
                    writer.WriteLine($"br ^{BlockId(sb.SuccessorBlock)}");
                    break;
                case ExitBlock eb:
                    // If we reach this point, it means the function has no return, we must manually add one
                    if (HasNoReturn(parentMethod))
                    {
                        writer.WriteLine("return");
                    }
                    else
                    {
                        writer.WriteLine("cbde.unreachable");
                    }
                    break;
            }
            writer.WriteLine();
        }

        private static SyntaxNode GetCondition(BranchBlock bbb)
        {
            // For an if or a while, bbb.BranchingNode represent the condition, not the statement that holds the condition
            // For a for, bbb.BranchingNode represents the for. Since for is a statement, not an expression, if we
            // see a for, we know it's at the top level of the expression tree, so it cannot be a for inside of a if condition
            switch (bbb.BranchingNode.Kind())
            {
                case SyntaxKind.ForStatement:
                    var forStmt = bbb.BranchingNode as ForStatementSyntax;
                    return forStmt.Condition;
                case SyntaxKind.ForEachStatement:
                    Debug.Assert(false, "Not ready to handle those");
                    return null;
                default:
                    return bbb.BranchingNode;
            }
        }

        private string GetArgumentsString(BaseMethodDeclarationSyntax method)
        {
            if (method.ParameterList.Parameters.Count == 0)
            {
                return string.Empty;
            }
            var paramCount = 0;
            var args = method.ParameterList.Parameters.Select(
                p =>
                {
                    ++paramCount;
                    var paramName = string.IsNullOrEmpty(p.Identifier.ValueText) ?
                        ".param" + paramCount.ToString() :
                        EncodeName(p.Identifier.ValueText);
                    return $"%{paramName} : {MlirType(p)}";
                }
                );
            return '(' + string.Join(", ", args) + ')';
        }

        private string GetAnonymousArgumentsString(BaseMethodDeclarationSyntax method)
        {
            var args = method.ParameterList.Parameters.Select(p => MlirType(p));
            return '(' + string.Join(", ", args) + ')';
        }

        private string MlirType(ParameterSyntax p)
        {
            var symbolType = semanticModel.GetDeclaredSymbol(p).GetSymbolType();
            return symbolType == null ? "none" : MlirType(symbolType);
        }

        private string MlirType(ExpressionSyntax e)
        {
            switch (e.RemoveParentheses().Kind())
            {
                case SyntaxKind.NullLiteralExpression:
                    return "none";
                case SyntaxKind.SimpleMemberAccessExpression:
                    var type = semanticModel.GetTypeInfo(e).Type;
                    if (type == null && !e.Parent.IsKind(SyntaxKind.InvocationExpression))
                    {
                        // Case of a method group that will get transformed into at Func<>, but does not have a type
                        return "none";
                    }
                    return MlirType(semanticModel.GetTypeInfo(e).Type);
                default:
                    return MlirType(semanticModel.GetTypeInfo(e).Type);
            }
        }

        private string MlirType(VariableDeclaratorSyntax v) => MlirType(semanticModel.GetDeclaredSymbol(v).GetSymbolType());

        private static string MlirType(ITypeSymbol csType)
        {
            Debug.Assert(csType != null);
            if (csType.SpecialType == SpecialType.System_Boolean)
            {
                return "i1";
            }
            else if (csType.SpecialType == SpecialType.System_Int32)
            {
                return "i32";
            }
            else
            {
                return "none";
            }
        }

        private static bool IsTypeKnown(ITypeSymbol csType) =>
            csType != null && (csType.SpecialType == SpecialType.System_Boolean || csType.SpecialType == SpecialType.System_Int32);

        private static ExpressionSyntax GetAssignmentValue(ExpressionSyntax rhs)
        {
            rhs = rhs.RemoveParentheses();
            while (rhs.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                rhs = (rhs as AssignmentExpressionSyntax).Right.RemoveParentheses();
            }
            return rhs;
        }

        private void ExportConstant(SyntaxNode op, ITypeSymbol type, string value)
        {
            if (type.SpecialType == SpecialType.System_Boolean)
            {
                value = Convert.ToInt32(value.ToLower() == "true").ToString();
            }

            if (!IsTypeKnown(type))
            {
                writer.WriteLine($"%{OpId(op)} = constant unit {GetLocation(op)} // {op.Dump()} ({op.Kind()})");
                return;
            }
            writer.WriteLine($"%{OpId(op)} = constant {value} : {MlirType(type)} {GetLocation(op)}");
        }

        private void ExtractInstruction(SyntaxNode op)
        {
            switch (op.Kind())
            {
                case SyntaxKind.RightShiftExpression:
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.ExclusiveOrExpression:
                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    var binExpr = op as BinaryExpressionSyntax;
                    ExtractBinaryExpression(binExpr, binExpr.Left, binExpr.Right);
                    return;
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                    ExtractBinaryAssignmentExpression(op);
                    return;
                case SyntaxKind.UnaryMinusExpression:
                    var neg = op as PrefixUnaryExpressionSyntax;
                    if (IsTypeKnown(semanticModel.GetTypeInfo(neg).Type) && !IsTypeKnown(semanticModel.GetTypeInfo(neg.Operand).Type))
                    {
                        writer.WriteLine($"%{OpId(neg)} = cbde.unknown : {MlirType(neg)} {GetLocation(op)} // A negation changing type whose source type is unknown");
                    }
                    else
                    {
                        writer.WriteLine($"%{OpId(neg)} = cbde.neg %{OpId(GetAssignmentValue(neg.Operand))} : {MlirType(neg)} {GetLocation(neg)}");
                    }
                    return;
                case SyntaxKind.UnaryPlusExpression:
                    var plus = op as PrefixUnaryExpressionSyntax;
                    opMap[plus] = opMap[GetAssignmentValue(plus.Operand)];
                    return;
                case SyntaxKind.TrueLiteralExpression:
                    writer.WriteLine($"%{OpId(op)} = constant 1 : i1 {GetLocation(op)} // true");
                    return;
                case SyntaxKind.FalseLiteralExpression:
                    writer.WriteLine($"%{OpId(op)} = constant 0 : i1 {GetLocation(op)} // false");
                    return;
                case SyntaxKind.NumericLiteralExpression:
                    var lit = op as LiteralExpressionSyntax;
                    ExportConstant(op, semanticModel.GetTypeInfo(lit).Type, lit.Token.ValueText);
                    return;
                case SyntaxKind.EqualsExpression:
                    ExportComparison("eq", op);
                    return;
                case SyntaxKind.NotEqualsExpression:
                    ExportComparison("ne", op);
                    return;
                case SyntaxKind.GreaterThanExpression:
                    ExportComparison("sgt", op);
                    return;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    ExportComparison("sge", op);
                    return;
                case SyntaxKind.LessThanExpression:
                    ExportComparison("slt", op);
                    return;
                case SyntaxKind.LessThanOrEqualExpression:
                    ExportComparison("sle", op);
                    return;
                case SyntaxKind.IdentifierName:
                    ExportIdentifierName(op);
                    return;
                case SyntaxKind.VariableDeclarator:
                    ExportVariableDeclarator(op as VariableDeclaratorSyntax);
                    return;
                case SyntaxKind.SimpleAssignmentExpression:
                    ExportSimpleAssignment(op);
                    return;
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PreDecrementExpression:
                    var prefixExp = op as PrefixUnaryExpressionSyntax;
                    ExportPrePostIncrementDecrement(prefixExp, prefixExp.OperatorToken, prefixExp.Operand, false);
                    return;
                case SyntaxKind.PostIncrementExpression:
                case SyntaxKind.PostDecrementExpression:
                    var postfixExp = op as PostfixUnaryExpressionSyntax;
                    ExportPrePostIncrementDecrement(postfixExp, postfixExp.OperatorToken, postfixExp.Operand, true);
                    return;
                case SyntaxKind.SimpleMemberAccessExpression:
                    var constant = semanticModel.GetConstantValue(op);
                    if (constant.HasValue && constant.Value is int intConstant) // Only Int32 types are currently supported by CBDE engine
                    {
                        ExportConstant(op, semanticModel.GetTypeInfo(op).Type, intConstant.ToString());
                        return;
                    }
                    break;
            }
            if (op is ExpressionSyntax expr && !(op.Kind() is SyntaxKind.NullLiteralExpression))
            {
                var exprType = semanticModel.GetTypeInfo(expr).Type;
                if (exprType == null)
                {
                    // Some intermediate expressions have no type (member access, initialization of member...)
                    // and therefore, they have no real value associated to them, we can just ignore them
                    return;
                }
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : {MlirType(exprType)} {GetLocation(op)} // {op.Dump()} ({op.Kind()})");
            }
            else
            {
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : none {GetLocation(op)} // {op.Dump()} ({op.Kind()})");
            }
        }

        private void ExportSimpleAssignment(SyntaxNode op)
        {
            var assign = op as AssignmentExpressionSyntax;
            if (!AreTypesSupported(assign))
            {
                return;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(assign.Left);
            if (!IsSymbolSupportedForAssignment(symbolInfo))
            {
                return;
            }

            var lhs = symbolInfo.Symbol.DeclaringSyntaxReferences[0].GetSyntax();
            var rhsType = semanticModel.GetTypeInfo(assign.Right).Type;
            string rhsId;
            if (rhsType.Kind == SymbolKind.ErrorType)
            {
                rhsId = UniqueOpId();
                writer.WriteLine($"%{rhsId} = cbde.unknown  : {MlirType(assign)}");
            }
            else
            {
                rhsId = ComputeCompatibleId(assign.Right, MlirType(assign));
            }

            writer.WriteLine($"cbde.store %{rhsId}, %{OpId(lhs)} : memref<{MlirType(assign)}> {GetLocation(op)}");
        }

        private static bool IsSymbolSupportedForAssignment(SymbolInfo symbolInfo) =>
            // We ignore the case where lhs is not a parameter or a local variable (ie field, property...) because we currently do not support these yet
            symbolInfo.Symbol != null && (symbolInfo.Symbol is ILocalSymbol || symbolInfo.Symbol is IParameterSymbol);

        private void ExportIdentifierName(SyntaxNode op)
        {
            var id = op as IdentifierNameSyntax;
            var declSymbol = semanticModel.GetSymbolInfo(id).Symbol;
            if (declSymbol == null)
            {
                // In case of an unresolved call, just skip it
                writer.WriteLine($"// Unresolved: {id.Identifier.ValueText}");
                return;
            }
            if (declSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                // The entity comes from another assembly
                // We can't ignore it if it is a property or a field because it may be used inside an operation (addi, subi, return...)
                // So if we ignore it, the next operation will use an unknown register
                // In case of a method, we can ignore it
                if (declSymbol is IPropertySymbol || declSymbol is IFieldSymbol)
                {
                    writer.WriteLine($"%{OpId(op)} = cbde.unknown : {MlirType(id)} {GetLocation(op)} // Identifier from another assembly: {id.Identifier.ValueText}");
                }
                else
                {
                    writer.WriteLine($"// Entity from another assembly: {id.Identifier.ValueText}");
                }
                return;
            }
            var decl = declSymbol.DeclaringSyntaxReferences[0].GetSyntax();
            if (decl == null ||                    // Not sure if we can be in this situation...
                decl is MethodDeclarationSyntax || // We will fetch the function only when looking at the function call itself
                decl is ClassDeclarationSyntax || // In "Class.member", we are not interested in the "Class" part
                decl is NamespaceDeclarationSyntax)
            {
                // We will fetch the function only when looking at the function call itself, we just skip the identifier
                writer.WriteLine($"// Skipped because MethodDeclarationSyntax or ClassDeclarationSyntax or NamespaceDeclarationSyntax: {id.Identifier.ValueText}");
                return;
            }

            if (declSymbol is INamespaceSymbol) // FileScopedNamespaceDeclaration will not be caught by the above `if`
            {
                writer.WriteLine($"// Skipped because FileScopedNamespaceDeclaration : {id.Identifier.ValueText}");
                return;
            }

            if (declSymbol is IFieldSymbol fieldSymbol && fieldSymbol.HasConstantValue)
            {
                var constValue = fieldSymbol.ConstantValue != null ? fieldSymbol.ConstantValue.ToString() : "null";
                ExportConstant(op, fieldSymbol.Type, constValue);
                return;
            }
            // IPropertySymbol could be either in a getter context (we should generate unknown) or in a setter
            // context (we should do nothing). However, it appears that in setter context, the CFG does not have an
            // instruction for fetching the property, so we should focus only on getter context.
            else if (declSymbol is IFieldSymbol || declSymbol is IPropertySymbol || !AreTypesSupported(id))
            {
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : {MlirType(id)} {GetLocation(op)} // Not a variable of known type: {id.Identifier.ValueText}");
                return;
            }
            writer.WriteLine($"%{OpId(op)} = cbde.load %{OpId(decl)} : memref<{MlirType(id)}> {GetLocation(op)}");
        }

        private void ExportVariableDeclarator(VariableDeclaratorSyntax declarator)
        {
            var id = OpId(declarator);
            if (!IsTypeKnown(semanticModel.GetDeclaredSymbol(declarator).GetSymbolType()))
            {
                // No need to write the variable, all references to it will be replaced by "unknown"
                return;
            }
            writer.WriteLine($"%{id} = cbde.alloca {MlirType(declarator)} {GetLocation(declarator)} // {declarator.Identifier.ValueText}");
            if (declarator.Initializer != null)
            {
                if (!AreTypesSupported(declarator.Initializer.Value))
                {
                    writer.WriteLine("// Initialized with unknown data");
                    return;
                }
                var value = GetAssignmentValue(declarator.Initializer.Value);
                writer.WriteLine($"cbde.store %{OpId(value)}, %{id} : memref<{MlirType(declarator)}> {GetLocation(declarator)}");
            }
        }

        private void ExtractBinaryExpression(ExpressionSyntax expr, ExpressionSyntax lhs, ExpressionSyntax rhs)
        {
            if (!AreTypesSupported(lhs, rhs, expr))
            {
                writer.WriteLine($"%{OpId(expr)} = cbde.unknown : {MlirType(expr)} {GetLocation(expr)} // Binary expression on unsupported types {expr.Dump()}");
                return;
            }
            string opName;
            switch (expr.Kind())
            {
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.RightShiftExpression:
                    var negateBitCountId = UniqueOpId();
                    writer.WriteLine($"%{negateBitCountId} = cbde.neg %{OpId(GetAssignmentValue(rhs))} : {MlirType(expr)} {GetLocation(expr)}");
                    writer.WriteLine($"%{OpId(expr)} = shlis %{OpId(GetAssignmentValue(lhs))}, %{negateBitCountId} : {MlirType(expr)} {GetLocation(expr)}");
                    return;
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.LeftShiftExpression:
                    opName = "shlis";
                    break;
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.BitwiseAndExpression:
                    opName = "and";
                    break;
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.BitwiseOrExpression:
                    opName = "or";
                    break;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.ExclusiveOrExpression:
                    opName = "xor";
                    break;
                case SyntaxKind.AddExpression:
                case SyntaxKind.AddAssignmentExpression:
                    opName = "addi";
                    break;
                case SyntaxKind.SubtractExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                    opName = "subi";
                    break;
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                    opName = "muli";
                    break;
                case SyntaxKind.DivideExpression:
                case SyntaxKind.DivideAssignmentExpression:
                    opName = "divis";
                    break;
                case SyntaxKind.ModuloExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                    opName = "remis";
                    break;
                default:
                    writer.WriteLine($"%{OpId(expr)} = cbde.unknown : {MlirType(expr)} {GetLocation(expr)} // Unknown operator {expr.Dump()}");
                    return;
            }
            writer.WriteLine($"%{OpId(expr)} = {opName} %{OpId(GetAssignmentValue(lhs))}, %{OpId(GetAssignmentValue(rhs))} : {MlirType(expr)} {GetLocation(expr)}");
        }

        private void ExtractBinaryAssignmentExpression(SyntaxNode op)
        {
            var assignExpr = op as AssignmentExpressionSyntax;
            ExtractBinaryExpression(assignExpr, assignExpr.Left, assignExpr.Right);

            if (!(assignExpr.Left is IdentifierNameSyntax id))
            {
                writer.WriteLine($"// No identifier name for binary assignment expression");
                return;
            }

            var declSymbol = semanticModel.GetSymbolInfo(id);
            if (!IsSymbolSupportedForAssignment(declSymbol) || !AreTypesSupported(assignExpr))
            {
                return;
            }

            var decl = declSymbol.Symbol.DeclaringSyntaxReferences[0].GetSyntax();
            writer.WriteLine($"cbde.store %{OpId(assignExpr)}, %{OpId(decl)} : memref<{MlirType(assignExpr)}> {GetLocation(op)}");
        }

        private void ExportPrePostIncrementDecrement(SyntaxNode op, SyntaxToken opToken, ExpressionSyntax operand, bool isPostOperation)
        {
            // For now we only handle IdentifierNameSyntax (not ElementAccessExpressionSyntax or other)
            if (!(operand is IdentifierNameSyntax id))
            {
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : {MlirType(operand)} {GetLocation(op)} // Inc/Decrement of unknown identifier");
                return;
            }

            var declSymbol = semanticModel.GetSymbolInfo(id);
            if (!IsSymbolSupportedForAssignment(declSymbol) || !AreTypesSupported(operand))
            {
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : {MlirType(operand)} {GetLocation(op)} // Inc/Decrement of field or property {id.Identifier.ValueText}");
                return;
            }

            var decl = declSymbol.Symbol.DeclaringSyntaxReferences[0].GetSyntax();
            if (isPostOperation)
            {
                opMap[op] = opMap[operand];
            }

            var newCstId = UniqueOpId();
            writer.WriteLine($"%{newCstId} = constant 1 : {MlirType(operand)} {GetLocation(op)}");

            var opId = isPostOperation ? UniqueOpId() : OpId(op);
            var opName = opToken.IsKind(SyntaxKind.PlusPlusToken) ? "addi" : "subi";
            writer.WriteLine($"%{opId} = {opName} %{OpId(operand)}, %{newCstId} : {MlirType(operand)} {GetLocation(op)}");

            writer.WriteLine($"cbde.store %{opId}, %{OpId(decl)} : memref<{MlirType(operand)}> {GetLocation(op)}");
        }

        private bool AreTypesSupported(params ExpressionSyntax[] exprs) =>
            exprs.All(expr => IsTypeKnown(semanticModel.GetTypeInfo(expr).Type));

        private void ExportComparison(string compName, SyntaxNode op)
        {
            var binExpr = op as BinaryExpressionSyntax;
            if (!AreTypesSupported(binExpr.Left, binExpr.Right))
            {
                writer.WriteLine($"%{OpId(op)} = cbde.unknown : i1  {GetLocation(binExpr)} // comparison of unknown type: {op.Dump()}");
                return;
            }
            // The type is the type of the operands, not of the result, which is always i1
            writer.WriteLine($"%{OpId(op)} = cmpi \"{compName}\", %{OpId(GetAssignmentValue(binExpr.Left))}, %{OpId(GetAssignmentValue(binExpr.Right))} : {MlirType(binExpr.Left)} {GetLocation(binExpr)}");
        }

        private string GetLocation(SyntaxNode node)
        {
            if (!exportsLocations)
            {
                return string.Empty;
            }

            // We should decide which one of GetLineSpan or GetMappedLineSpan is better to use
            // See: https://github.com/SonarSource/SonarCBDE/issues/30
            var loc = node.GetLocation().GetLineSpan();
            var location = $"loc(\"{loc.Path}\"" +
                $" :{loc.StartLinePosition.Line}" +
                $" :{loc.StartLinePosition.Character})";

            return location.Replace("\\", "\\\\");
        }

        private string ComputeCompatibleId(ExpressionSyntax op, string resultType)
        {
            if (MlirType(op) == resultType)
            {
                return OpId(GetAssignmentValue(op));
            }
            var newId = UniqueOpId();
            writer.WriteLine($"%{newId} = cbde.unknown : {resultType} {GetLocation(op)} // Dummy variable because type of %{OpId(GetAssignmentValue(op))} is incompatible");
            return newId;
        }

        public int BlockId(Block cfgBlock) =>
            blockMap.GetOrAdd(cfgBlock, b => blockCounter++);

        public string OpId(SyntaxNode node) =>
            opMap.GetOrAdd(node.RemoveParentheses(), b => opCounter++).ToString();

        // In some cases, we need an OpId that referes to a boolean variable, even if the variable happens not to be
        // a boolean (for instance, it could be a dynamic). In such a case, we just create an unknown bool...
        // Beware not to call this function in the middle of writing some text, because it can add some of its own
        public string EnforceBoolOpId(ExpressionSyntax e)
        {
            if (MlirType(e) != "i1")
            {
                var newId = UniqueOpId();
                writer.WriteLine($"%{newId} = cbde.unknown : i1 // Creating necessary bool for conversion");
                return newId;
            }
            return OpId(GetAssignmentValue(e));
        }

        public string UniqueOpId() =>
            (opCounter++).ToString();

        public string EncodeName(string name) =>
            '_' + encoder.GetString(encoder.GetBytes(name)); // Enforce encoder fallback configuration
    }

    internal static class SyntaxNodeExtension
    {
        public static string Dump(this SyntaxNode node) =>
            Regex.Replace(node.ToString(), @"\t|\n|\r", " ");
    }
}

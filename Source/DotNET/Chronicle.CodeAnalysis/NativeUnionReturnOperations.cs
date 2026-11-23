// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Finds visibly empty union values in returns, without interpreting opaque locals or factories.
/// </summary>
static class NativeUnionReturnOperations
{
    /// <summary>
    /// Finds event types in statically empty union return expressions.
    /// </summary>
    /// <param name="method">The handler to inspect.</param>
    /// <param name="compilation">The compilation containing the handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event types carried by visibly empty unions.</returns>
    internal static IEnumerable<ITypeSymbol> EmptyEvents(IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken) =>
        method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken)).OfType<MethodDeclarationSyntax>()
            .SelectMany(declaration => EmptyEvents(declaration, compilation, cancellationToken));

    static IEnumerable<ITypeSymbol> EmptyEvents(MethodDeclarationSyntax declaration, Compilation compilation, CancellationToken cancellationToken)
    {
        var model = compilation.GetSemanticModel(declaration.SyntaxTree);
        return ReturnExpressions(declaration).SelectMany(expression =>
            EmptyEvents(WithReturnConversion(model.GetOperation(expression, cancellationToken)), compilation, 0));
    }

    static IEnumerable<ExpressionSyntax> ReturnExpressions(MethodDeclarationSyntax declaration) =>
        declaration.ExpressionBody is { } expressionBody
            ? [expressionBody.Expression]
            : declaration.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).OfType<ExpressionSyntax>();

    static IOperation? WithReturnConversion(IOperation? operation)
    {
        while (operation?.Parent is IConversionOperation conversion)
        {
            operation = conversion;
        }
        return operation;
    }

    static IEnumerable<ITypeSymbol> EmptyEvents(IOperation? operation, Compilation compilation, int depth)
    {
        if (operation is null || depth >= NativeUnionSymbols.MaximumDepth)
        {
            return [];
        }
        if (IsEmptyUnion(operation))
        {
            return NullableCommandEventReturnAnalyzer.EventTypes(operation.Type!, compilation);
        }
        return ReturnValues(operation, compilation).SelectMany(value => EmptyEvents(value, compilation, depth + 1));
    }

    static bool IsEmptyUnion(IOperation operation) => NativeUnionSymbols.IsUnion(operation.Type) &&
        (operation is IDefaultValueOperation || IsImplicitEmptyCreation(operation));

    static bool IsImplicitEmptyCreation(IOperation operation) =>
        operation is IObjectCreationOperation { Arguments.Length: 0 } creation &&
        creation.Constructor?.IsImplicitlyDeclared != false;

    static IEnumerable<IOperation> ReturnValues(IOperation operation, Compilation compilation) => operation switch
    {
        IConversionOperation conversion => ConversionValues(conversion, compilation),
        IObjectCreationOperation creation => CreationValues(creation, compilation),
        IInvocationOperation invocation => InvocationValues(invocation, compilation),
        _ => BranchValues(operation)
    };

    static IEnumerable<IOperation> BranchValues(IOperation operation) => operation switch
    {
        ITupleOperation tuple => tuple.Elements,
        IConditionalOperation conditional => new[] { conditional.WhenTrue, conditional.WhenFalse }.OfType<IOperation>(),
        ISwitchExpressionOperation expression => expression.Arms.Select(arm => arm.Value),
        _ => []
    };

    static IEnumerable<IOperation> ConversionValues(IConversionOperation conversion, Compilation compilation) =>
        IsTransparent(conversion, compilation) ? [conversion.Operand] : [];

    static IEnumerable<IOperation> CreationValues(IObjectCreationOperation creation, Compilation compilation) =>
        IsCaseOrAwaitableCreation(creation, compilation) ? creation.Arguments.Select(argument => argument.Value) : [];

    static IEnumerable<IOperation> InvocationValues(IInvocationOperation invocation, Compilation compilation) =>
        IsFromResult(invocation, compilation) ? invocation.Arguments.Select(argument => argument.Value) : [];

    static bool IsTransparent(IConversionOperation conversion, Compilation compilation) =>
        !conversion.Conversion.IsUserDefined ||
        (conversion.Type is INamedTypeSymbol named &&
         (RawGuidResponseAnalysis.IsType(named, $"OneOf.OneOf`{named.Arity}", compilation) ||
          RawGuidResponseAnalysis.IsType(named, "Cratis.Monads.Result`2", compilation)));

    static bool IsCaseOrAwaitableCreation(IObjectCreationOperation creation, Compilation compilation) =>
        creation.Constructor is { } constructor &&
        ((NativeUnionSymbols.IsUnion(creation.Type) && NativeUnionSymbols.IsCaseConstructor(constructor)) ||
         RawGuidResponseAnalysis.IsType(creation.Type, "System.Threading.Tasks.ValueTask`1", compilation));

    static bool IsFromResult(IInvocationOperation invocation, Compilation compilation) =>
        invocation.TargetMethod.Name == "FromResult" &&
        RawGuidResponseAnalysis.IsType(invocation.TargetMethod.ContainingType, "System.Threading.Tasks.Task", compilation);
}

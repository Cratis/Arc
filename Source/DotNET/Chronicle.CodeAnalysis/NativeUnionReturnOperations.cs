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
    internal static IEnumerable<ITypeSymbol> EmptyEvents(IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken)
    {
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not MethodDeclarationSyntax declaration)
            {
                continue;
            }
            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            var expressions = declaration.ExpressionBody is { } expressionBody
                ? [expressionBody.Expression]
                : declaration.DescendantNodes(node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                    .OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).OfType<ExpressionSyntax>();
            foreach (var expression in expressions)
            {
                foreach (var eventType in EmptyEvents(WithReturnConversion(model.GetOperation(expression, cancellationToken)), compilation, 0))
                {
                    yield return eventType;
                }
            }
        }
    }

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
        (operation is IDefaultValueOperation || operation is IObjectCreationOperation { Arguments.Length: 0 });

    static IEnumerable<IOperation> ReturnValues(IOperation operation, Compilation compilation) => operation switch
    {
        IConversionOperation conversion when IsTransparent(conversion, compilation) => [conversion.Operand],
        ITupleOperation tuple => tuple.Elements,
        IConditionalOperation conditional => new[] { conditional.WhenTrue, conditional.WhenFalse }.OfType<IOperation>(),
        ISwitchExpressionOperation expression => expression.Arms.Select(arm => arm.Value),
        IObjectCreationOperation creation when IsCaseOrAwaitableCreation(creation, compilation) =>
            creation.Arguments.Select(argument => argument.Value),
        IInvocationOperation invocation when IsFromResult(invocation, compilation) =>
            invocation.Arguments.Select(argument => argument.Value),
        _ => []
    };

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

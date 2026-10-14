// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reads only the returns of a visible callback, not helpers or nested functions.
/// </summary>
static class RefusalCallbackReturns
{
    /// <summary>
    /// Gets the values a lambda, anonymous method or source method-group callback returns.
    /// </summary>
    /// <param name="callback">The callback argument.</param>
    /// <param name="compilation">The compilation resolving source method bodies.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The visible returned values.</returns>
    internal static IEnumerable<IOperation> Read(IOperation callback, Compilation compilation, CancellationToken cancellationToken) => Unwrap(callback) switch
    {
        IAnonymousFunctionOperation function => ReturnsIn(function.Body),
        IMethodReferenceOperation reference => reference.Method.DeclaringSyntaxReferences
            .Select(syntax => syntax.GetSyntax(cancellationToken))
            .SelectMany(declaration => ReturnsOf(declaration, compilation, cancellationToken)),
        _ => []
    };

    static IOperation Unwrap(IOperation callback) => callback switch
    {
        IDelegateCreationOperation creation => Unwrap(creation.Target),
        IConversionOperation { OperatorMethod: null } conversion => Unwrap(conversion.Operand),
        _ => callback
    };

    static SyntaxNode? BodyOf(SyntaxNode declaration) => declaration switch
    {
        MethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        _ => null
    };

    static IEnumerable<IOperation> ReturnsOf(SyntaxNode declaration, Compilation compilation, CancellationToken cancellationToken)
    {
        if (BodyOf(declaration) is not { } body || !compilation.ContainsSyntaxTree(body.SyntaxTree))
        {
            return [];
        }

        var operation = compilation.GetSemanticModel(body.SyntaxTree).GetOperation(body, cancellationToken);

        return operation switch
        {
            IBlockOperation block => ReturnsIn(block),
            not null => [operation],
            _ => []
        };
    }

    static IEnumerable<IOperation> ReturnsIn(IOperation operation)
    {
        if (operation is IReturnOperation { ReturnedValue: { } value })
        {
            yield return value;
            yield break;
        }

        foreach (var child in operation.ChildOperations.Where(child => child is not IAnonymousFunctionOperation and not ILocalFunctionOperation))
        {
            foreach (var returned in ReturnsIn(child))
            {
                yield return returned;
            }
        }
    }
}

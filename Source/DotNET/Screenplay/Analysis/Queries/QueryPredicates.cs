// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Finds predicates on the returned LINQ or Mongo query, not unrelated lambdas elsewhere in its body.
/// </summary>
internal static class QueryPredicates
{
    static readonly string[] _materializers = ["ToList", "ToArray", "ToListAsync", "ToArrayAsync", "AsQueryable", "AsEnumerable", "ToEnumerable"];

    /// <summary>
    /// Reads the predicates in the returned query chain.
    /// </summary>
    /// <param name="declaration">The query declaration.</param>
    /// <param name="model">The semantic model resolving query methods.</param>
    /// <returns>The returned query's predicates.</returns>
    internal static IEnumerable<LambdaExpressionSyntax> Of(MethodDeclarationSyntax declaration, SemanticModel model)
    {
        var returned = declaration.ExpressionBody?.Expression ?? SingleReturn(declaration.Body);

        return returned is null ? [] : In(returned, model);
    }

    /// <summary>
    /// Removes parentheses and awaiting without changing the returned value.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <returns>The unwrapped expression.</returns>
    internal static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parentheses => Unwrap(parentheses.Expression),
        AwaitExpressionSyntax awaited => Unwrap(awaited.Expression),
        _ => expression
    };

    static ExpressionSyntax? SingleReturn(BlockSyntax? body) => body?.Statements is [ReturnStatementSyntax returned] ? returned.Expression : null;

    static IEnumerable<LambdaExpressionSyntax> In(ExpressionSyntax expression, SemanticModel model)
    {
        if (Unwrap(expression) is not InvocationExpressionSyntax invocation ||
            model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || !IsQueryOperation(method))
        {
            return [];
        }

        var predicates = IsPredicate(method.Name)
            ? invocation.ArgumentList.Arguments.Select(argument => Unwrap(argument.Expression)).OfType<LambdaExpressionSyntax>()
            : [];
        var source = SourceOf(invocation, method);

        return source is null ? predicates : predicates.Concat(In(source, model));
    }

    static bool IsQueryOperation(IMethodSymbol method) =>
        IsQueryNamespace(method.ContainingNamespace.ToDisplayString()) &&
        (IsPredicate(method.Name) || _materializers.Contains(method.Name, StringComparer.Ordinal));

    static bool IsQueryNamespace(string name) => name == "System.Linq" || name.StartsWith("MongoDB.Driver", StringComparison.Ordinal);

    static bool IsPredicate(string name) => name.Equals("Where", StringComparison.Ordinal) || name.Equals("Find", StringComparison.Ordinal);

    static ExpressionSyntax? SourceOf(InvocationExpressionSyntax invocation, IMethodSymbol method) =>
        method.IsStatic && method.ReducedFrom is null
            ? invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression
            : (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
}

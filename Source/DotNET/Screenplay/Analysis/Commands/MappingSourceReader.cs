// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads where the value of an expression inside a command handler comes from.
/// </summary>
/// <remarks>
/// Only two sources survive into a document - a path into the command's own input, and a constant. Anything else is
/// code, and a mapping guessing at code would be worse than a mapping that is not there.
/// <para>
/// An expression read from a body the handler called rather than from the handler itself names that body's
/// parameters, so the bindings of the call site stand in for them and the value is followed back to the command.
/// </para>
/// </remarks>
public static class MappingSourceReader
{
    /// <summary>
    /// Reads the source of an expression.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The source, or <see langword="null"/> when the expression is not expressible.</returns>
    public static MappingSourceModel? Read(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ParameterBindings? bindings = null)
    {
        var unwrapped = Unwrap(expression);

        var constant = semanticModel.GetConstantValue(unwrapped);
        if (constant.HasValue)
        {
            return new LiteralSource(constant.Value);
        }

        if (bindings?.Resolve(unwrapped, semanticModel) is { } bound)
        {
            return Read(bound.Expression, bound.SemanticModel, owner);
        }

        var path = ReadPath(unwrapped, semanticModel, owner, bindings);

        return path is null ? null : new PropertyPathSource(path);
    }

    /// <summary>
    /// Reads the dotted path an expression walks into the command's own input.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The path, or <see langword="null"/> when the expression does not walk into the input.</returns>
    public static string? ReadPath(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ParameterBindings? bindings = null)
    {
        var segments = new List<string>();
        var current = Unwrap(expression);

        while (true)
        {
            switch (current)
            {
                case MemberAccessExpressionSyntax member:
                    segments.Insert(0, member.Name.Identifier.ValueText);
                    current = Unwrap(member.Expression);
                    continue;

                case ThisExpressionSyntax:
                    return segments.Count == 0 ? null : string.Join('.', segments);

                case IdentifierNameSyntax identifier when bindings?.Resolve(identifier, semanticModel) is { } bound:
                    return ReadPath(bound.Expression, bound.SemanticModel, owner) is { } prefix
                        ? string.Join('.', segments.Prepend(prefix))
                        : null;

                case IdentifierNameSyntax identifier:
                    segments.Insert(0, identifier.Identifier.ValueText);

                    return IsOwnInput(identifier, semanticModel, owner) ? string.Join('.', segments) : null;

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Determines whether an identifier names a property of the command itself.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the identifier lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <returns>True when the identifier resolves to the command's own input.</returns>
    static bool IsOwnInput(IdentifierNameSyntax identifier, SemanticModel semanticModel, ITypeSymbol owner) =>
        semanticModel.GetSymbolInfo(identifier).Symbol is IPropertySymbol property &&
        SymbolEqualityComparer.Default.Equals(property.ContainingType, owner);

    /// <summary>
    /// Strips the wrappers that do not change what an expression yields.
    /// </summary>
    /// <param name="expression">The expression to strip.</param>
    /// <returns>The wrapped expression.</returns>
    static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        CastExpressionSyntax cast => Unwrap(cast.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.SuppressNullableWarningExpression } suppress =>
            Unwrap(suppress.Operand),
        _ => expression
    };
}

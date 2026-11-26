// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves literal prefixes and invariant casing around direct String generation strategies.
/// </summary>
internal static class GenerationOptionsReader
{
    static readonly string[] _prefixKinds = ["uuid", "uuidv7", "ulid", "nanoid", "bytes"];

    /// <summary>
    /// Recovers only wrappers whose effects are represented by Screenplay's strategy modifiers.
    /// </summary>
    /// <param name="expression">The primitive returned by a forwarding concept factory.</param>
    /// <param name="model">The model proving the core call and wrapper symbols.</param>
    /// <returns>The strategy with its proven options, or null.</returns>
    internal static GenerationModel? Read(ExpressionSyntax expression, SemanticModel model)
    {
        var (core, prefix) = PrefixOf(GenerationCallReader.UnwrapParentheses(expression));
        var casing = CaseOf(core, model);
        var generation = StrategyOf(core, model);
        if (generation is null || !AllowsOptions(generation, prefix, casing))
        {
            return null;
        }

        return generation with { Prefix = prefix ?? generation.Prefix, Case = casing == DefaultCase(generation) ? null : casing };
    }

    /// <summary>
    /// Refuses invalid literal modifiers so they cannot fall back to an inline rule.
    /// </summary>
    /// <param name="expression">The returned primitive expression.</param>
    /// <param name="model">The model proving the strategy.</param>
    /// <returns>The primitive, or null when its literal prefix is invalid.</returns>
    internal static ExpressionSyntax? WithValidPrefix(ExpressionSyntax? expression, SemanticModel model) =>
        expression is not null && !HasInvalidPrefix(expression, model) ? expression : null;

    static bool HasInvalidPrefix(ExpressionSyntax expression, SemanticModel model)
    {
        var (core, prefix) = PrefixOf(GenerationCallReader.UnwrapParentheses(expression));

        if (prefix is null || ValidPrefix(prefix))
        {
            return false;
        }

        return StrategyOf(core, model) is { } generation && _prefixKinds.Contains(generation.Kind, StringComparer.Ordinal);
    }

    static GenerationModel? StrategyOf(ExpressionSyntax core, SemanticModel model)
    {
        if (CaseOf(core, model) is not null)
        {
            core = GenerationCallReader.UnwrapParentheses(((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)core).Expression).Expression);
        }

        return DirectStrategy(UuidTextOf(core, model), model);
    }

    static (ExpressionSyntax Core, string? Prefix) PrefixOf(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax { Left: LiteralExpressionSyntax { Token.Value: string literal } } binary && binary.IsKind(SyntaxKind.AddExpression))
        {
            return (GenerationCallReader.UnwrapParentheses(binary.Right), literal);
        }

        return (expression, null);
    }

    static GenerationModel? DirectStrategy(ExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetSymbolInfo(expression).Symbol is not IMethodSymbol method || !method.ContainingType.Is("Cratis.Concepts.GenerateValue"))
        {
            return null;
        }

        return GenerationCallReader.Read(expression, model);
    }

    static bool AllowsOptions(GenerationModel generation, string? prefix, string? casing) =>
        AllowsPrefix(generation, prefix) && AllowsCase(generation, casing);

    static bool AllowsPrefix(GenerationModel generation, string? prefix) =>
        prefix is null || (ValidPrefix(prefix) && _prefixKinds.Contains(generation.Kind, StringComparer.Ordinal));

    static bool ValidPrefix(string prefix) => prefix.Length <= 32 && prefix.All(character => character is >= ' ' and <= '~');

    static bool AllowsCase(GenerationModel generation, string? casing) =>
        casing is null || DefaultCase(generation) is not null;

    static string? DefaultCase(GenerationModel generation) => generation.Kind switch
    {
        "uuid" or "uuidv7" => "lower",
        "ulid" => "upper",
        "bytes" when generation.Encoding == "hex" => "lower",
        "bytes" when generation.Encoding == "base32" => "upper",
        _ => null
    };

    static string? CaseOf(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax, ArgumentList.Arguments.Count: 0 } call)
        {
            return null;
        }
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol { IsStatic: false, Parameters.Length: 0 } method ||
            method.ContainingType.SpecialType != SpecialType.System_String)
        {
            return null;
        }

        return method.Name switch
        {
            "ToLowerInvariant" => "lower",
            "ToUpperInvariant" => "upper",
            _ => null
        };
    }

    static ExpressionSyntax UuidTextOf(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } call && IsUuidText(call, model))
        {
            return GenerationCallReader.UnwrapParentheses(member.Expression);
        }

        return expression;
    }

    static bool IsUuidText(InvocationExpressionSyntax call, SemanticModel model) =>
        IsDefaultFormat(call) && model.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
        method.Name == "ToString" && method.ContainingType.Is("System.Guid");

    static bool IsDefaultFormat(InvocationExpressionSyntax call) => call.ArgumentList.Arguments.Count == 0 ||
        call.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax { Token.Value: "D" } }];
}

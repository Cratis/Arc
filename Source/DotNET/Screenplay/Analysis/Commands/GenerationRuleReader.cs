// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recovers self-contained String factories for the Cratis C# ValueGeneration binding.
/// </summary>
/// <param name="forms">The shared constructor-forwarding proof.</param>
internal class GenerationRuleReader(UuidGenerationForms forms)
{
    static readonly HashSet<string> _strategies = new(StringComparer.Ordinal) { "Uuid", "UuidV7", "Ulid", "TypeId", "NanoId", "Bytes", "Pattern", "Integer" };

    /// <summary>
    /// Copies a closed body, removing only proven forwarding concept constructions.
    /// </summary>
    /// <param name="type">The concept owning the factory.</param>
    /// <param name="method">The source factory.</param>
    /// <param name="model">The model resolving every dependency.</param>
    /// <returns>The primitive-returning rule, or null for an unproved dependency.</returns>
    public GenerationModel? Read(INamedTypeSymbol type, MethodDeclarationSyntax method, SemanticModel model)
    {
        if (type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is not [{ SpecialType: SpecialType.System_String }])
        {
            return null;
        }
        var body = (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression;

        return CanRead(body, model) ? ReadBody(type, method, model) : null;
    }

    static bool IsUnrecoveredStrategy(ExpressionSyntax primitive, SemanticModel model) =>
        GenerationCallReader.UnwrapParentheses(primitive) is InvocationExpressionSyntax call &&
        model.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
        ((method.ContainingType.Is("Cratis.Concepts.GenerateValue") && _strategies.Contains(method.Name)) ||
         (method.ContainingType.Is("System.Security.Cryptography.RandomNumberGenerator") && method.Name == "GetHexString"));

    static bool CanRead(SyntaxNode? body, SemanticModel model)
    {
        if (body?.ContainsDirectives != false)
        {
            return false;
        }

        return !model.GetDiagnostics(body.Span).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    static ExpressionSyntax?[] ReturnsOf(MethodDeclarationSyntax method) => method.Body is { } block
        ? block.DescendantNodes().OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).ToArray()
        : [method.ExpressionBody?.Expression];

    static string BodyText(SyntaxNode stripped) => stripped is BlockSyntax block
        ? (block.OpenBraceToken.TrailingTrivia.ToFullString() + string.Concat(block.Statements.Select(statement => statement.ToFullString())) + block.CloseBraceToken.LeadingTrivia.ToFullString()).Trim()
        : $"return {stripped};";

    static bool HasPrimitiveReturns(ExpressionSyntax?[] primitives) => primitives.Length > 0 && primitives.All(expression => expression is not null);

    static bool CanCopy(SyntaxNode body, INamedTypeSymbol type, SemanticModel model, ExpressionSyntax[] primitives) =>
        !primitives.Any(primitive => IsUnrecoveredStrategy(primitive, model)) && new GenerationRuleDependencies(type, model).Allows(body, primitives);

    GenerationModel? ReadBody(INamedTypeSymbol type, MethodDeclarationSyntax method, SemanticModel model)
    {
        var returns = ReturnsOf(method);
        var primitives = returns.Select(expression => expression is null ? null : forms.PrimitiveOf(expression, type, model)).ToArray();
        if (!HasPrimitiveReturns(primitives))
        {
            return null;
        }
        var expressions = primitives.Cast<ExpressionSyntax>().ToArray();
        var body = (SyntaxNode?)method.Body ?? method.ExpressionBody!.Expression;
        if (!CanCopy(body, type, model, expressions))
        {
            return null;
        }
        var stripped = body.ReplaceNodes(returns.Cast<ExpressionSyntax>(), (original, _) => forms.PrimitiveOf(original, type, model)!.WithTriviaFrom(original));

        return new("rule") { Rule = $"{type.Name}New", Body = BodyText(stripped) };
    }
}

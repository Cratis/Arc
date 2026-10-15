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
    static readonly HashSet<string> _types = new(StringComparer.Ordinal)
    {
        "System.String", "System.Math", "System.Convert", "System.Guid",
        "System.Security.Cryptography.RandomNumberGenerator", "Cratis.Concepts.GenerateValue",
        "Cratis.Concepts.Checksum", "Cratis.Concepts.ValueEncoding"
    };

    static readonly HashSet<string> _strategies = new(StringComparer.Ordinal) { "Uuid", "UuidV7", "Ulid", "TypeId", "NanoId", "Bytes", "Pattern", "Integer" };
    static readonly HashSet<string> _contextMembers = new(StringComparer.Ordinal) { "Now", "Tenant" };

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
        if (body?.ContainsDirectives != false || model.GetDiagnostics(body.Span).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return null;
        }
        var returns = method.Body?.DescendantNodes().OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).ToArray()
            ?? [method.ExpressionBody?.Expression];
        if (returns.Length == 0 || returns.Any(expression => expression is null || forms.PrimitiveOf(expression, type, model) is null))
        {
            return null;
        }
        var primitives = returns.Select(expression => forms.PrimitiveOf(expression!, type, model)!).ToArray();
        if (primitives.Any(primitive => IsUnrecoveredStrategy(primitive, model)) || !IsClosed(body, type, model, primitives))
        {
            return null;
        }
        var stripped = body.ReplaceNodes(returns.Cast<ExpressionSyntax>(), (original, _) => forms.PrimitiveOf(original, type, model)!.WithTriviaFrom(original));
        var text = method.Body is null ? $"return {primitives[0]};" : stripped.ToFullString().Trim()[1..^1].Trim();

        return new("rule") { Rule = $"{type.Name}New", Body = text };
    }

    static bool IsUnrecoveredStrategy(ExpressionSyntax primitive, SemanticModel model) =>
        GenerationCallReader.UnwrapParentheses(primitive) is InvocationExpressionSyntax call &&
        model.GetSymbolInfo(call).Symbol is IMethodSymbol method &&
        ((method.ContainingType.Is("Cratis.Concepts.GenerateValue") && _strategies.Contains(method.Name)) ||
         (method.ContainingType.Is("System.Security.Cryptography.RandomNumberGenerator") && method.Name == "GetHexString"));

    static bool IsClosed(SyntaxNode body, INamedTypeSymbol type, SemanticModel model, ExpressionSyntax[] primitives)
    {
        if (body.DescendantNodesAndSelf().Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or AwaitExpressionSyntax or TypeOfExpressionSyntax or DirectiveTriviaSyntax))
        {
            return false;
        }

        return body.DescendantNodesAndSelf().OfType<ExpressionSyntax>().All(node => Allowed(node, type, model, primitives));
    }

    static bool Allowed(ExpressionSyntax node, INamedTypeSymbol concept, SemanticModel model, ExpressionSyntax[] primitives)
    {
        if (node is LiteralExpressionSyntax literal && literal.Token.ValueText == "Default")
        {
            return false;
        }
        if (node is BaseObjectCreationExpressionSyntax creation)
        {
            return primitives.Any(primitive => primitive.Parent?.Parent?.Parent == creation) ||
                (model.GetTypeInfo(creation).Type is INamedTypeSymbol created && IsAllowedType(created));
        }
        var symbol = model.GetSymbolInfo(node).Symbol;

        return symbol switch
        {
            IParameterSymbol parameter => AllowedContext(parameter, node, model),
            ILocalSymbol local => BodyOwns(local, node) && (local.Type.SpecialType != SpecialType.None || local.Type.Is("System.DateTimeOffset") || (local.Type is INamedTypeSymbol named && IsAllowedType(named))),
            IMethodSymbol or IPropertySymbol or IFieldSymbol => AllowedMember(symbol),
            INamedTypeSymbol named => IsAllowedType(named) || named.Is("System.DateTimeOffset") || named.SpecialType != SpecialType.None || SymbolEqualityComparer.Default.Equals(named, concept),
            IAliasSymbol => false,
            _ => node is not IdentifierNameSyntax || node.Parent is MemberAccessExpressionSyntax or NameColonSyntax
        };
    }

    static bool AllowedContext(IParameterSymbol parameter, ExpressionSyntax node, SemanticModel model) =>
        parameter.Name == "context" && parameter.Type.Is("Cratis.Concepts.GenerationContext") &&
        ((node.Parent is MemberAccessExpressionSyntax member && _contextMembers.Contains(member.Name.Identifier.ValueText)) ||
         (node.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax call } &&
          model.GetSymbolInfo(call).Symbol is IMethodSymbol method && method.ContainingType.Is("Cratis.Concepts.GenerateValue")));

    static bool BodyOwns(ILocalSymbol local, SyntaxNode node) => local.DeclaringSyntaxReferences.Any(reference =>
        reference.SyntaxTree == node.SyntaxTree && reference.GetSyntax().Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() == node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault());

    static bool IsAllowedType(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.IsEmpty && _types.Contains(type.FullMetadataName());

    static bool AllowedMember(ISymbol symbol)
    {
        if (symbol.ContainingType.Is("Cratis.Concepts.GenerationContext"))
        {
            return symbol is IPropertySymbol && _contextMembers.Contains(symbol.Name);
        }
        if (symbol.ContainingType.Is("System.DateTimeOffset"))
        {
            return !symbol.IsStatic && symbol.DeclaringSyntaxReferences.IsEmpty;
        }

        return IsAllowedType(symbol.ContainingType) && symbol.DeclaringSyntaxReferences.IsEmpty;
    }
}

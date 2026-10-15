// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recognizes generation calls and proven primitive-to-concept conversions.
/// </summary>
/// <param name="models">The models resolving concept bodies.</param>
internal class UuidGenerationForms(SemanticModels models)
{
    readonly UuidConstructorForwarding _constructors = new(models);

    /// <summary>
    /// Gets the single expression a method returns.
    /// </summary>
    /// <param name="declaration">The method or conversion declaration.</param>
    /// <returns>The returned expression, or null for a multi-statement body.</returns>
    public static ExpressionSyntax? ReturnedBy(BaseMethodDeclarationSyntax declaration) => declaration.ExpressionBody?.Expression ??
        (declaration.Body?.Statements is [ReturnStatementSyntax statement] ? statement.Expression : null);

    /// <summary>
    /// Reads a strategy only when its primitive reaches the concept unchanged.
    /// </summary>
    /// <param name="expression">The factory expression.</param>
    /// <param name="type">The expected concept.</param>
    /// <param name="model">The model resolving the expression.</param>
    /// <returns>The proven strategy, or null.</returns>
    public GenerationModel? Strategy(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        if (PrimitiveOf(expression, type, model) is not { } primitive)
        {
            return null;
        }
        var backing = type.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments.SingleOrDefault()?.SpecialType;
        if (!AllowsCasts(primitive, backing))
        {
            return null;
        }
        var generation = GenerationCallReader.Read(primitive, model);

        return HasMatchingBacking(generation, primitive, backing) ? generation : null;
    }

    /// <summary>
    /// Removes only a constructor or conversion proved to forward the primitive unchanged.
    /// </summary>
    /// <param name="expression">The returned concept expression.</param>
    /// <param name="type">The concept being constructed.</param>
    /// <param name="model">The model resolving its constructor.</param>
    /// <returns>The primitive expression, or null for an unproved conversion.</returns>
    public ExpressionSyntax? PrimitiveOf(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        expression = GenerationCallReader.UnwrapParentheses(expression);

        return expression switch
        {
            BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation when ConstructsConcept(creation, type, model) => argument.Expression,
            _ when ThroughConversion(expression, type, model) => expression,
            _ => null
        };
    }

    static bool AllowsCasts(ExpressionSyntax primitive, SpecialType? backing)
    {
        var casts = primitive.DescendantNodesAndSelf().OfType<CastExpressionSyntax>().ToArray();
        if (casts.Length == 0)
        {
            return true;
        }
        if (backing != SpecialType.System_Int32)
        {
            return false;
        }

        return casts.Length == 1 && GenerationCallReader.UnwrapParentheses(primitive) == casts[0];
    }

    static bool HasMatchingBacking(GenerationModel? generation, ExpressionSyntax primitive, SpecialType? backing)
    {
        if (generation?.Kind != "random")
        {
            return true;
        }

        return backing switch
        {
            SpecialType.System_Int64 => true,
            SpecialType.System_Int32 => GenerationCallReader.UnwrapParentheses(primitive) is CastExpressionSyntax,
            _ => false
        };
    }

    static bool IsForwardedParameter(ExpressionSyntax expression, IParameterSymbol parameter, SemanticModel model) =>
        model.GetConversion(expression).IsIdentity && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, parameter);

    bool ConstructsConcept(BaseObjectCreationExpressionSyntax creation, INamedTypeSymbol type, SemanticModel model)
    {
        if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructor || !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, type))
        {
            return false;
        }

        return _constructors.Read(constructor, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
    }

    bool ThroughConversion(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        if (model.GetConversion(expression).MethodSymbol is not { Parameters: [var parameter] } method || !SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
        {
            return false;
        }

        return method.DeclaringSyntaxReferences.Any(reference => ConversionForwards(reference.GetSyntax(), type, parameter));
    }

    bool ConversionForwards(SyntaxNode syntax, INamedTypeSymbol type, IParameterSymbol parameter)
    {
        if (syntax is not ConversionOperatorDeclarationSyntax declaration || models.For(declaration.SyntaxTree) is not { } model)
        {
            return false;
        }
        if (ReturnedBy(declaration) is not BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation)
        {
            return false;
        }

        return ConstructsConcept(creation, type, model) && IsForwardedParameter(argument.Expression, parameter, model);
    }
}

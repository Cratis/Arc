// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recognizes direct UUID creation and proven primitive-to-concept conversions.
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
    /// Proves a recognized UUID v4 expression constructs the given concept.
    /// </summary>
    /// <param name="expression">The factory expression.</param>
    /// <param name="type">The expected concept.</param>
    /// <param name="model">The model resolving the expression.</param>
    /// <returns>Whether the concept receives a fresh, unchanged UUID v4.</returns>
    public bool Read(ExpressionSyntax expression, INamedTypeSymbol type, SemanticModel model)
    {
        if (expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax))
        {
            return false;
        }

        return expression switch
        {
            BaseObjectCreationExpressionSyntax { Initializer: null, ArgumentList.Arguments: [var argument] } creation =>
                ConstructsConcept(creation, type, model) && IsUuidCall(argument.Expression, model),
            _ => ThroughConversion(expression, type, model)
        };
    }

    static bool IsUuidCall(ExpressionSyntax expression, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } call ||
            model.GetSymbolInfo(call).Symbol is not IMethodSymbol { IsStatic: true, Parameters.Length: 0 } method)
        {
            return false;
        }

        return method.Name switch
        {
            "NewGuid" => method.ContainingType.Is("System.Guid"),
            "Uuid" => method.ContainingType.Is("Cratis.Concepts.GenerateValue"),
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
        if (!IsUuidCall(expression, model))
        {
            return false;
        }
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

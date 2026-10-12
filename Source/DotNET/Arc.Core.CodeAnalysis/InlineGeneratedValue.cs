// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Identifies expressions that can be replaced by a generatable concept parameter.
/// </summary>
internal static class InlineGeneratedValue
{
    static readonly string[] _unsupportedArtifactAttributes =
    [
        "Cratis.Arc.Queries.ModelBound.ReadModelAttribute",
        "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
        "Microsoft.AspNetCore.Mvc.ControllerAttribute"
    ];

    /// <summary>
    /// Checks the supported model-bound Handle location.
    /// </summary>
    /// <param name="syntax">The method declaration.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>Whether the method is a command Handle.</returns>
    internal static bool IsCommandHandle(MethodDeclarationSyntax syntax, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(syntax) is not IMethodSymbol { Name: "Handle", IsStatic: false, IsGenericMethod: false } method)
        {
            return false;
        }

        var types = Hierarchy(method.ContainingType).ToArray();

        return types.Any(type => HasAttribute(type, "Cratis.Arc.Commands.ModelBound.CommandAttribute")) && !types.Any(IsUnsupportedArtifact);
    }

    /// <summary>
    /// Finds the complete concept-valued generation expression.
    /// </summary>
    /// <param name="invocation">The generation invocation.</param>
    /// <param name="model">The semantic model.</param>
    /// <returns>The replaceable expression, or null.</returns>
    internal static ExpressionSyntax? ExpressionFor(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { IsStatic: true } method)
        {
            return null;
        }

        if (IsConceptFactory(invocation, method))
        {
            return invocation;
        }

        if (!IsPrimitiveGeneration(method))
        {
            return null;
        }

        return ConceptExpression(WrapperFor(invocation), model);
    }

    /// <summary>
    /// Checks for a self-generatable concept.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <returns>Whether the type is a self-generatable concept.</returns>
    internal static bool IsConcept(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        return HasConceptBase(type) && type.AllInterfaces.Any(contract => IsSelfGenerationContract(contract, type));
    }

    /// <summary>
    /// Checks for a nested execution boundary.
    /// </summary>
    /// <param name="node">The candidate node.</param>
    /// <returns>Whether the node is a nested function.</returns>
    internal static bool IsNestedFunction(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

    static ExpressionSyntax WrapperFor(InvocationExpressionSyntax invocation) => invocation.Parent switch
    {
        ArgumentSyntax { Parent: ArgumentListSyntax { Arguments.Count: 1, Parent: ObjectCreationExpressionSyntax creation } } => creation,
        ArgumentSyntax { Parent: ArgumentListSyntax { Arguments.Count: 1, Parent: ImplicitObjectCreationExpressionSyntax creation } } => creation,
        CastExpressionSyntax cast => cast,
        _ => invocation
    };

    static bool IsConceptFactory(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        if (method.Name != "New" || invocation.ArgumentList.Arguments.Count != 0)
        {
            return false;
        }

        return IsConcept(method.ReturnType) && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.ContainingType);
    }

    static ExpressionSyntax? ConceptExpression(ExpressionSyntax expression, SemanticModel model)
    {
        var type = model.GetTypeInfo(expression);
        var isConcept = IsConcept(type.Type) || IsConcept(type.ConvertedType);

        return isConcept ? expression : null;
    }

    static bool HasConceptBase(ITypeSymbol type) =>
        Hierarchy(type).Any(candidate => candidate.OriginalDefinition.ToDisplayString() == "Cratis.Concepts.ConceptAs<T>");

    static bool IsSelfGenerationContract(INamedTypeSymbol contract, ITypeSymbol type) =>
        IsGenerationInterface(contract) && SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], type);

    static bool IsGenerationInterface(INamedTypeSymbol contract) =>
        contract.OriginalDefinition.ToDisplayString() == "Cratis.Concepts.IGeneratable<TSelf>" ||
        contract.OriginalDefinition.ToDisplayString() == "Cratis.Concepts.IGeneratableInContext<TSelf>";

    static bool IsUnsupportedArtifact(ITypeSymbol type) => type.ToDisplayString() == "Microsoft.AspNetCore.Mvc.ControllerBase" ||
        type.GetAttributes().Any(IsUnsupportedAttribute);

    static bool IsUnsupportedAttribute(AttributeData attribute) =>
        _unsupportedArtifactAttributes.Contains(attribute.AttributeClass?.ToDisplayString() ?? string.Empty, StringComparer.Ordinal);

    static bool IsPrimitiveGeneration(IMethodSymbol method) => method.ContainingType.ToDisplayString() == "Cratis.Concepts.GenerateValue" || IsGuidGeneration(method);

    static bool IsGuidGeneration(IMethodSymbol method)
    {
        if (method.ContainingType.SpecialType != SpecialType.None)
        {
            return false;
        }

        return method.ContainingType.ToDisplayString() == "System.Guid" && method.Name == "NewGuid";
    }

    static bool HasAttribute(ITypeSymbol type, string name) => type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == name);

    static IEnumerable<ITypeSymbol> Hierarchy(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }
}

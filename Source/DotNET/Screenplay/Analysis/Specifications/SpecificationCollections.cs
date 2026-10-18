// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads a collection fixture only when every element has an exact literal value.
/// </summary>
static class SpecificationCollections
{
    /// <summary>
    /// Reads the complete ordered collection, or nothing when any element cannot be stated.
    /// </summary>
    /// <param name="expression">The collection expression.</param>
    /// <param name="model">The semantic model resolving the expression.</param>
    /// <param name="type">The declared fixture property type.</param>
    /// <param name="sources">The scalar literal reader.</param>
    /// <returns>The collection literal, or null when it cannot be read in full.</returns>
    internal static LiteralSource? Read(ExpressionSyntax expression, SemanticModel model, ITypeSymbol type, MappingSourceReader sources)
    {
        if (CollectionElements.ElementOf(type) is not { } elementType ||
            Elements(MappingSourceReader.Unwrap(expression), model) is not { } elements)
        {
            return null;
        }

        var values = new List<object?>();
        foreach (var element in elements)
        {
            var constant = model.GetConstantValue(MappingSourceReader.Unwrap(element));
            if (constant is { HasValue: true, Value: not null } && StatableValues.TryState(elementType, constant.Value, out var value))
            {
                values.Add(value);
            }
            else if (element is BaseObjectCreationExpressionSyntax && sources.ReadQueryLiteral(element, model, elementType, elementType) is { Value: not null } concept)
            {
                values.Add(concept.Value);
            }
            else
            {
                return null;
            }
        }

        return new(values.ToArray());
    }

    static IEnumerable<ExpressionSyntax>? Elements(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        CollectionExpressionSyntax collection when collection.Elements.All(element => element is ExpressionElementSyntax) =>
            collection.Elements.Cast<ExpressionElementSyntax>().Select(element => element.Expression),
        ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
        ArrayCreationExpressionSyntax { Type.RankSpecifiers: [var rank], Initializer: { } initializer } when rank.Rank == 1 => initializer.Expressions,
        BaseObjectCreationExpressionSyntax creation when IsListConstruction(creation, model) => creation.Initializer?.Expressions ?? [],
        InvocationExpressionSyntax invocation when IsArrayEmpty(invocation, model) => [],
        _ => null
    };

    static bool IsListConstruction(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        (creation.ArgumentList?.Arguments.Count ?? 0) == 0 &&
        model.GetTypeInfo(creation).Type is INamedTypeSymbol named &&
        named.OriginalDefinition.FullMetadataName() == "System.Collections.Generic.List`1";

    static bool IsArrayEmpty(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 0 &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true, Arity: 1, Parameters.Length: 0 } method &&
        string.Equals(method.Name, "Empty", StringComparison.Ordinal) &&
        method.ContainingType.SpecialType == SpecialType.System_Array;
}

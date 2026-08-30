// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads exact direct event-property values from an appended-event predicate.
/// </summary>
static class SpecificationEventPredicateValues
{
    const string PredicateParameter = "predicate";

    /// <summary>
    /// Reads every exact event value stated by an optional predicate.
    /// </summary>
    /// <param name="invocation">The appended-event assertion.</param>
    /// <param name="method">The exactly bound assertion method.</param>
    /// <param name="eventType">The event type the assertion names.</param>
    /// <param name="semanticModel">The semantic model owning the assertion.</param>
    /// <param name="draft">The scenario collecting exact value evidence.</param>
    /// <param name="values">The values in authored predicate order.</param>
    /// <param name="reason">The blocking reason when the predicate is not exact.</param>
    /// <returns><see langword="true"/> when the predicate is absent or every value is exact.</returns>
    public static bool TryRead(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        ITypeSymbol eventType,
        SemanticModel semanticModel,
        SpecificationDraft draft,
        out IReadOnlyList<PropertyMappingModel> values,
        out string? reason)
    {
        values = [];
        reason = null;
        var predicateParameters = DotNetInvocations.DefinitionOf(method).Parameters
            .Where(_ => string.Equals(_.Name, PredicateParameter, StringComparison.Ordinal))
            .ToArray();
        if (predicateParameters.Length == 0)
        {
            return true;
        }

        if (predicateParameters.Length != 1 ||
            DotNetInvocations.ArgumentForParameter(invocation, method, PredicateParameter, semanticModel)?.Expression is not LambdaExpressionSyntax predicate ||
            predicate.Body is not ExpressionSyntax body ||
            ParameterOf(predicate, semanticModel) is not { } parameter ||
            !SymbolEqualityComparer.Default.Equals(parameter.Type, eventType))
        {
            reason = "an expected event predicate is not an exact single-parameter expression";
            return false;
        }

        var recovered = new List<(PropertyMappingModel Value, Location Source)>();
        var properties = new HashSet<string>(StringComparer.Ordinal);
        if (!TryRead(body, eventType, parameter, semanticModel, recovered, properties))
        {
            reason = "an expected event predicate is not a conjunction of direct event-property equalities to exact constants";
            return false;
        }

        foreach (var (value, source) in recovered)
        {
            draft.AddValue(value, source);
        }

        values = [.. recovered.Select(_ => _.Value)];
        return true;
    }

    static IParameterSymbol? ParameterOf(LambdaExpressionSyntax predicate, SemanticModel semanticModel) => predicate switch
    {
        SimpleLambdaExpressionSyntax simple => semanticModel.GetDeclaredSymbol(simple.Parameter),
        ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var parameter] } => semanticModel.GetDeclaredSymbol(parameter),
        _ => null
    };

    static bool TryRead(
        ExpressionSyntax expression,
        ITypeSymbol eventType,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        List<(PropertyMappingModel Value, Location Source)> values,
        HashSet<string> properties)
    {
        expression = Unwrap(expression);
        if (expression is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } conjunction)
        {
            return TryRead(conjunction.Left, eventType, parameter, semanticModel, values, properties) &&
                   TryRead(conjunction.Right, eventType, parameter, semanticModel, values, properties);
        }

        if (expression is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } equality)
        {
            return false;
        }

        return TryRead(equality.Left, equality.Right, eventType, parameter, semanticModel, values, properties) ||
               TryRead(equality.Right, equality.Left, eventType, parameter, semanticModel, values, properties);
    }

    static bool TryRead(
        ExpressionSyntax memberExpression,
        ExpressionSyntax valueExpression,
        ITypeSymbol eventType,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        List<(PropertyMappingModel Value, Location Source)> values,
        HashSet<string> properties)
    {
        memberExpression = Unwrap(memberExpression);
        valueExpression = Unwrap(valueExpression);
        if (memberExpression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member ||
            !SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(receiver).Symbol, parameter) ||
            semanticModel.GetSymbolInfo(member).Symbol is not IPropertySymbol property ||
            !SymbolEqualityComparer.Default.Equals(property.ContainingType, eventType) ||
            semanticModel.GetConstantValue(valueExpression) is not { HasValue: true } constant ||
            !properties.Add(property.Name))
        {
            return false;
        }

        values.Add((new(property.Name, new LiteralSource(constant.Value)), valueExpression.GetLocation()));
        return true;
    }

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}

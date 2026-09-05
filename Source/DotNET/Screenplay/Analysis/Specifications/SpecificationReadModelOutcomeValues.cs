// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads exact values asserted against a generated read-model scenario instance.
/// </summary>
static class SpecificationReadModelOutcomeValues
{
    const string ScenarioType = "Cratis.Chronicle.Testing.ReadModels.ReadModelScenario`1";
    const string AssertionType = "Cratis.Specifications.ShouldEqualityExtensions";

    /// <summary>
    /// Reads the exact generated read-model outcome and its source evidence.
    /// </summary>
    /// <param name="type">The authored scenario type.</param>
    /// <param name="steps">The type carrying the scenario fields and methods.</param>
    /// <param name="readModel">The exact read model type.</param>
    /// <param name="models">The semantic models owning the scenario bodies.</param>
    /// <param name="draft">The scenario collecting exact value evidence.</param>
    /// <param name="values">The values in read-model declaration order.</param>
    /// <param name="source">The exact read-model type-argument source.</param>
    /// <param name="reason">The blocking reason when the outcome is not exact.</param>
    /// <returns><see langword="true"/> when every public read-model property is asserted exactly once.</returns>
    public static bool TryRead(
        INamedTypeSymbol type,
        INamedTypeSymbol steps,
        INamedTypeSymbol readModel,
        SemanticModels models,
        SpecificationDraft draft,
        out IReadOnlyList<PropertyMappingModel> values,
        out Location? source,
        out string? reason)
    {
        values = [];
        source = null;
        reason = null;
        var scenarioFields = steps.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => IsScenario(field.Type, readModel))
            .ToArray();
        if (scenarioFields.Length != 1 || TypeArgumentSource(scenarioFields[0]) is not { } scenarioSource)
        {
            reason = "the read-model scenario field or its exact type argument is missing or ambiguous";
            return false;
        }

        var expectedProperties = readModel.DeclaredProperties().ToArray();
        var recovered = new Dictionary<string, (PropertyMappingModel Value, Location Source)>(StringComparer.Ordinal);
        foreach (var assertion in SpecificationMembers.AssertionsIn(type))
        {
            foreach (var body in HandlerBodies.Of(assertion))
            {
                if (models.For(body.SyntaxTree) is not { } semanticModel)
                {
                    reason = "an assertion has no exact semantic model";
                    return false;
                }

                foreach (var invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    var method = DotNetInvocations.MethodFor(invocation, semanticModel);
                    var receiver = method is null ? null : DotNetInvocations.ReceiverFor(invocation, method, semanticModel);
                    if (!TryReadProperty(receiver, semanticModel, scenarioFields[0], expectedProperties, out var property))
                    {
                        continue;
                    }

                    if (!StepsTaken.Always(invocation, body))
                    {
                        reason = $"the read-model property '{property!.Name}' is asserted conditionally";
                        return false;
                    }

                    if (!IsExactEquality(method!))
                    {
                        reason = $"the read-model property '{property!.Name}' does not use the exact allowlisted equality assertion";
                        return false;
                    }

                    if (DotNetInvocations.ArgumentForParameter(invocation, method!, "expected", semanticModel)?.Expression is not { } expected ||
                        semanticModel.GetConstantValue(expected) is not { HasValue: true } constant)
                    {
                        reason = $"the read-model property '{property!.Name}' is not asserted with an exact constant";
                        return false;
                    }

                    var value = constant.Value;
                    if (EnumConstants.IsEnumeration(property!.Type))
                    {
                        if (!EnumConstants.TryResolve(property.Type, constant.Value, out var enumeration))
                        {
                            reason = $"the read-model property '{property.Name}' names no exact enumeration member";
                            return false;
                        }

                        value = enumeration;
                    }

                    if (!recovered.TryAdd(property.Name, (new(property.Name, new LiteralSource(value)), expected.GetLocation())))
                    {
                        reason = $"the read-model property '{property.Name}' is asserted repeatedly";
                        return false;
                    }
                }
            }
        }

        if (expectedProperties.Length == 0 ||
            recovered.Count != expectedProperties.Length ||
            expectedProperties.Any(property => !recovered.ContainsKey(property.Name)))
        {
            reason = "the generated scenario does not assert every public read-model property exactly once";
            return false;
        }

        var ordered = expectedProperties.Select(property => recovered[property.Name]).ToArray();
        foreach (var item in ordered)
        {
            draft.AddValue(item.Value, item.Source);
        }

        values = [.. ordered.Select(_ => _.Value)];
        source = scenarioSource;
        return true;
    }

    static bool IsScenario(ITypeSymbol type, INamedTypeSymbol readModel) =>
        type is INamedTypeSymbol { TypeArguments: [var argument] } named &&
        $"{named.OriginalDefinition.ContainingNamespace.ToDisplayString()}.{named.OriginalDefinition.MetadataName}" == ScenarioType &&
        SymbolEqualityComparer.Default.Equals(argument, readModel);

    static Location? TypeArgumentSource(IFieldSymbol field) =>
        field.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .Select(variable => variable.Parent as VariableDeclarationSyntax)
            .SelectMany(declaration => declaration?.Type.DescendantNodesAndSelf().OfType<GenericNameSyntax>() ?? [])
            .Where(generic => generic.Identifier.ValueText == "ReadModelScenario" && generic.TypeArgumentList.Arguments.Count == 1)
            .Select(generic => generic.TypeArgumentList.Arguments[0].GetLocation())
            .SingleOrDefault();

    static bool TryReadProperty(
        ExpressionSyntax? expression,
        SemanticModel semanticModel,
        IFieldSymbol scenarioField,
        IReadOnlyList<IPropertySymbol> expectedProperties,
        out IPropertySymbol? property)
    {
        property = null;
        expression = expression is null ? null : Unwrap(expression);
        if (expression is not MemberAccessExpressionSyntax member ||
            semanticModel.GetSymbolInfo(member).Symbol is not IPropertySymbol candidate ||
            !expectedProperties.Any(property => SymbolEqualityComparer.Default.Equals(property, candidate)))
        {
            return false;
        }

        if (Unwrap(member.Expression) is not MemberAccessExpressionSyntax instance ||
            semanticModel.GetSymbolInfo(instance).Symbol is not IPropertySymbol { Name: "Instance" } ||
            !SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(Unwrap(instance.Expression)).Symbol, scenarioField))
        {
            return false;
        }

        property = candidate;
        return true;
    }

    static bool IsExactEquality(IMethodSymbol method)
    {
        var definition = DotNetInvocations.DefinitionOf(method);
        return definition.ContainingType.ToDisplayString() == AssertionType &&
            definition.Name == "ShouldEqual" &&
            definition.IsExtensionMethod &&
            definition.TypeParameters.Length == 1 &&
            definition.Parameters is [var actual, var expected] &&
            SymbolEqualityComparer.Default.Equals(actual.Type, definition.TypeParameters[0]) &&
            SymbolEqualityComparer.Default.Equals(expected.Type, definition.TypeParameters[0]);
    }

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    break;
                case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed:
                    expression = suppressed.Operand;
                    break;
                default:
                    return expression;
            }
        }
    }
}

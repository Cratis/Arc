// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads concrete generation pins while refusing configurations with no portable counterpart.
/// </summary>
/// <param name="models">The models resolving setup bodies.</param>
/// <param name="held">The one-hop held-value reader.</param>
internal class GeneratedValuePins(SemanticModels models, HeldValues held)
{
    /// <summary>
    /// Recovers named fixtures from unconditional setup calls.
    /// </summary>
    /// <param name="steps">The specification setup chain.</param>
    /// <param name="specification">The recovered scenario.</param>
    /// <returns>The scenario with its generation pins or a refusal reason.</returns>
    public SpecificationModel Read(INamedTypeSymbol steps, SpecificationModel specification)
    {
        var command = SpecificationMembers.CommandOf(steps);
        var values = new List<PropertyMappingModel>();
        string? failure = null;
        var methods = SpecificationMembers.MethodsIn(steps, SpecificationMembers.EstablishMethod)
            .Concat(SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod));
        foreach (var body in methods.SelectMany(HandlerBodies.Of))
        {
            if (models.For(body.SyntaxTree) is not { } model)
            {
                continue;
            }
            foreach (var call in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || !SpecificationCalls.IsGenerationConfiguration(method))
                {
                    continue;
                }

                if (method.Name == "AllowGeneration")
                {
                    failure = "AllowGeneration() has no concrete fixture; use PinGeneratedValue with a literal UUID";
                    continue;
                }
                var target = method.TypeArguments.LastOrDefault();
                var receiver = call.Expression is MemberAccessExpressionSyntax member ? model.GetTypeInfo(member.Expression).Type : null;
                var scenarioCommand = receiver is INamedTypeSymbol { TypeArguments: [var issued] } scenario && scenario.Is(WellKnownTypeNames.CommandScenario) ? issued : method.TypeArguments.FirstOrDefault();
                if (command is null || specification.When?.Name != command.Name || !SymbolEqualityComparer.Default.Equals(command, scenarioCommand) ||
                    (target is not null && !SymbolEqualityComparer.Default.Equals(target, command)))
                {
                    failure = "a nested-command or other-command pin cannot be represented; pin only the issued command";
                    continue;
                }
                var nameExpression = CallArguments.For(call, method, "name").SingleOrDefault();
                var valueExpression = CallArguments.For(call, method, "value").SingleOrDefault();
                var name = nameExpression is null ? null : model.GetConstantValue(nameExpression).Value as string;
                var parameter = command.GetMembers("Handle").OfType<IMethodSymbol>().SelectMany(handler => handler.Parameters)
                    .SingleOrDefault(parameter => parameter.GetAttributes().Any(attribute => attribute.AttributeClass.Is(WellKnownTypeNames.GeneratedValueAttribute) &&
                        (attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter.Name) == name));
                var literal = valueExpression is null ? null : Literal(valueExpression, model);
                if (name is null || parameter is null || valueExpression is null || !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(valueExpression).Type, parameter.Type) ||
                    literal is not { Value: string text } || !Guid.TryParse(text, out var guid) ||
                    !StatableValues.TryState(parameter.Type, text, out _) || values.Exists(value => value.Property == name) ||
                    !InlineProductionShape.IsUnconditional(body) || call.Ancestors().Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax) ||
                    body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(execution => execution.SpanStart < call.SpanStart &&
                        model.GetSymbolInfo(execution).Symbol is IMethodSymbol executed && SpecificationCalls.IsExecution(executed)))
                {
                    failure = $"PinGeneratedValue('{name ?? "?"}') is not a single unconditional literal UUID pin before execution; replace New() or computed values with a literal pin";
                    continue;
                }
                values.Add(new(name, new LiteralSource(guid.ToString("D"))));
            }
        }

        return specification with { GeneratedValues = values, GenerationFailure = failure };
    }

    LiteralSource? Literal(ExpressionSyntax expression, SemanticModel model)
    {
        var literal = SpecificationEventSources.ReadLiteral(expression, model);
        if (literal is not null)
        {
            return literal;
        }

        var symbol = model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol;
        return symbol is not null && held.InitializerOf(symbol, model.Compilation) is { } initializer && models.For(initializer.SyntaxTree) is { } initializerModel
            ? SpecificationEventSources.ReadLiteral(initializer, initializerModel)
            : null;
    }
}

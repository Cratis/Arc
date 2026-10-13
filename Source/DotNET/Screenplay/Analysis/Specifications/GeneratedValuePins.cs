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
        foreach (var configuration in methods.SelectMany(HandlerBodies.Of).SelectMany(ConfigurationsIn))
        {
            var pin = ReadPin(configuration, command, specification.When?.Name, values);
            if (pin.Value is { } value)
            {
                values.Add(value);
            }
            failure = pin.Failure ?? failure;
        }

        return specification with { GeneratedValues = values, GenerationFailure = failure };
    }

    static PinResult InvalidPin(string? name) => new(
        null,
        $"PinGeneratedValue('{name ?? "?"}') is not a single unconditional literal UUID or text pin before execution; replace New() or computed values with a literal pin");

    static string? NameOf(ConfigurationCall configuration)
    {
        var expression = CallArguments.For(configuration.Call, configuration.Method, "name").SingleOrDefault();

        return expression is null ? null : configuration.Model.GetConstantValue(expression).Value as string;
    }

    static IParameterSymbol? ParameterOf(ITypeSymbol command, string name) => command.GetMembers("Handle").OfType<IMethodSymbol>().SelectMany(handler => handler.Parameters)
        .SingleOrDefault(parameter => parameter.GetAttributes().Any(attribute => IsNamedGeneration(attribute, parameter.Name, name)));

    static bool IsNamedGeneration(AttributeData attribute, string parameter, string name) =>
        attribute.AttributeClass.Is(WellKnownTypeNames.GeneratedValueAttribute) && (attribute.ConstructorArguments.FirstOrDefault().Value as string ?? parameter) == name;

    static ITypeSymbol? ScenarioCommandOf(ConfigurationCall configuration)
    {
        var receiver = configuration.Call.Expression is MemberAccessExpressionSyntax member ? configuration.Model.GetTypeInfo(member.Expression).Type : null;
        if (receiver is INamedTypeSymbol { TypeArguments: [var issued] } scenario && scenario.Is(WellKnownTypeNames.CommandScenario))
        {
            return issued;
        }

        return configuration.Method.TypeArguments.FirstOrDefault();
    }

    static bool TargetsIssuedCommand(ConfigurationCall configuration, ITypeSymbol? command, string? issued)
    {
        if (command is null || issued != command.Name)
        {
            return false;
        }
        if (!SymbolEqualityComparer.Default.Equals(command, ScenarioCommandOf(configuration)))
        {
            return false;
        }

        var target = configuration.Method.TypeArguments.LastOrDefault();

        return target is null || SymbolEqualityComparer.Default.Equals(target, command);
    }

    static bool IsBeforeExecution(ConfigurationCall configuration)
    {
        if (!InlineProductionShape.IsUnconditional(configuration.Body))
        {
            return false;
        }
        if (configuration.Call.Ancestors().Any(node => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
        {
            return false;
        }

        return !configuration.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(call => IsEarlierExecution(call, configuration));
    }

    static bool IsEarlierExecution(InvocationExpressionSyntax call, ConfigurationCall configuration)
    {
        if (call.SpanStart >= configuration.Call.SpanStart)
        {
            return false;
        }

        return configuration.Model.GetSymbolInfo(call).Symbol is IMethodSymbol method && SpecificationCalls.IsExecution(method);
    }

    IEnumerable<ConfigurationCall> ConfigurationsIn(SyntaxNode body)
    {
        if (models.For(body.SyntaxTree) is not { } model)
        {
            return [];
        }

        return body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(call => (Call: call, Method: model.GetSymbolInfo(call).Symbol as IMethodSymbol))
            .Where(_ => _.Method is not null && SpecificationCalls.IsGenerationConfiguration(_.Method))
            .Select(_ => new ConfigurationCall(body, _.Call, _.Method!, model));
    }

    PinResult ReadPin(ConfigurationCall configuration, ITypeSymbol? command, string? issued, IReadOnlyList<PropertyMappingModel> values)
    {
        if (configuration.Method.Name == "AllowGeneration")
        {
            return new(null, "AllowGeneration() has no concrete fixture; use PinGeneratedValue with a literal UUID or text value");
        }
        if (!TargetsIssuedCommand(configuration, command, issued))
        {
            return new(null, "a nested-command or other-command pin cannot be represented; pin only the issued command");
        }

        var name = NameOf(configuration);
        if (StrategyFailureOf(command!, name) is { } failure)
        {
            return new(null, failure);
        }
        var value = ReadValue(configuration, command!, name);
        if (value is null)
        {
            return InvalidPin(name);
        }
        if (values.Any(value => value.Property == name) || !IsBeforeExecution(configuration))
        {
            return InvalidPin(name);
        }

        return new(value, null);
    }

    string? StrategyFailureOf(ITypeSymbol command, string? name)
    {
        if (name is null || ParameterOf(command, name) is not { } parameter)
        {
            return null;
        }

        return new GenerationStrategyReader(models).Failure(parameter) is not null
            ? $"the generation strategy of '{name}' is not recoverable ({ScreenplayDiagnosticCodes.GenerationNotRecoverable})"
            : null;
    }

    PropertyMappingModel? ReadValue(ConfigurationCall configuration, ITypeSymbol command, string? name)
    {
        if (name is null || ParameterOf(command, name) is not { } parameter)
        {
            return null;
        }
        var expression = CallArguments.For(configuration.Call, configuration.Method, "value").SingleOrDefault();
        if (expression is null || parameter.Type is not INamedTypeSymbol concept)
        {
            return null;
        }
        var source = ReadConceptValue(configuration.Model, expression, concept);

        return source is null ? null : new(name, source);
    }

    LiteralSource? ReadConceptValue(SemanticModel model, ExpressionSyntax expression, INamedTypeSymbol concept)
    {
        if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(expression).Type, concept))
        {
            return null;
        }
        if (Literal(expression, model) is not { Value: string text })
        {
            return null;
        }

        return ReadTextValue(concept, text);
    }

    LiteralSource? ReadTextValue(INamedTypeSymbol concept, string text)
    {
        var generation = new GenerationStrategyReader(models).Read(concept);
        if (generation is null || !GenerationFixtureShape.Matches(generation, text))
        {
            return null;
        }
        var value = concept.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments is [var backing] && backing.Is("System.Guid")
            ? Guid.Parse(text).ToString("D") : text;

        return StatableValues.TryState(concept, value, out _) ? new LiteralSource(value) : null;
    }

    LiteralSource? Literal(ExpressionSyntax expression, SemanticModel model) =>
        SpecificationEventSources.ReadLiteral(expression, model) ?? HeldLiteral(expression, model);

    LiteralSource? HeldLiteral(ExpressionSyntax expression, SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol;
        if (symbol is null || held.InitializerOf(symbol, model.Compilation) is not { } initializer)
        {
            return null;
        }

        return models.For(initializer.SyntaxTree) is { } initializerModel ? SpecificationEventSources.ReadLiteral(initializer, initializerModel) : null;
    }

    sealed record ConfigurationCall(SyntaxNode Body, InvocationExpressionSyntax Call, IMethodSymbol Method, SemanticModel Model);

    sealed record PinResult(PropertyMappingModel? Value, string? Failure);
}

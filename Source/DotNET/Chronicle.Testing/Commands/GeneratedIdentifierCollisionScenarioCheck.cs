// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Refuses collision fixtures whose prior events are absent from the command log.
/// </summary>
/// <param name="context">The scenario context containing its read mode.</param>
/// <param name="readModels">The default mode's seeded read model state.</param>
internal sealed class GeneratedIdentifierCollisionScenarioCheck(IDictionary<string, object> context, CommandScenarioReadModels readModels) : ICommandScenarioExecutionCheck
{
    /// <inheritdoc/>
    public void Verify(Type commandType, ScenarioGeneratedValueFixture fixture)
    {
        if (context.ContainsKey(ChronicleCommandScenarioExtender.DecisionScenarioKey))
        {
            return;
        }

        var parameters = commandType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == "Handle")
            .SelectMany(method => method.GetParameters());
        if (parameters.Any(parameter => IsSeededGuardedPin(commandType, parameter, fixture)))
        {
            throw new GeneratedIdentifierCollisionRequiresDecisionReads();
        }
    }

    bool IsSeededGuardedPin(Type commandType, ParameterInfo parameter, ScenarioGeneratedValueFixture fixture)
    {
        var attribute = parameter.GetCustomAttribute<GeneratedValueAttribute>();
        if (attribute is null || attribute.Freshness != GeneratedValueFreshness.Guarded ||
            !fixture.TryGetPin(commandType, attribute.Name ?? parameter.Name!, out var value) ||
            !parameter.ParameterType.IsInstanceOfType(value))
        {
            return false;
        }

        var identifier = GeneratedIdentifierCanonicalizer.Canonicalize(value);

        return identifier is not null && readModels.HasSeededEventsFor(identifier);
    }
}

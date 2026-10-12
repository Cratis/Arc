// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Admits named generation fixtures and keeps occurrences on the pinned identity implicit.
/// </summary>
/// <param name="diagnostics">Where withheld scenarios are reported.</param>
internal class SpecificationGenerationPins(ScreenplayDiagnostics? diagnostics)
{
    /// <summary>
    /// Applies a scenario's generation pins to its emitted command.
    /// </summary>
    /// <param name="specification">The recovered scenario.</param>
    /// <param name="command">The admitted command, when present.</param>
    /// <param name="location">The scenario's diagnostic location.</param>
    /// <returns>The pinned scenario, or null when a pin cannot be represented.</returns>
    public SpecificationModel? Apply(SpecificationModel specification, CommandModel? command, string? location)
    {
        var failure = FailureOf(specification, command);
        if (failure is not null)
        {
            diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because {failure}", location);
            return null;
        }

        return command?.Authoring is { GeneratedParameters: true, Generated.Count: > 0 } generation ? WithIdentity(specification, generation.Identifier) : specification;
    }

    static string? FailureOf(SpecificationModel specification, CommandModel? command)
    {
        if (specification.GenerationFailure is { } failure)
        {
            return failure;
        }
        if (specification.GeneratedValues.Any(value => !IsAdmitted(value, command)))
        {
            return "its pinned generated values are not admitted by the emitted command; use a supported UUID concept and an ESM v7 or later cap";
        }
        if (command?.Authoring is not { GeneratedParameters: true, Generated.Count: > 0 } generation)
        {
            return null;
        }

        var missing = generation.Generated.Where(property => specification.GeneratedValues.All(value => value.Property != property.Name)).Select(property => property.Name).ToList();

        return !specification.Errors.Any() && missing.Count > 0
            ? $"generated values '{string.Join(", ", missing)}' need literal UUID fixtures; use PinGeneratedValue before execution"
            : null;
    }

    static bool IsAdmitted(PropertyMappingModel value, CommandModel? command) => command?.Authoring?.Generated.Any(property => property.Name == value.Property) == true;

    static SpecificationModel WithIdentity(SpecificationModel specification, string? identifier)
    {
        if (specification.GeneratedValues.SingleOrDefault(value => value.Property == identifier)?.Source is not LiteralSource pin)
        {
            return specification;
        }

        return specification with
        {
            When = specification.When! with { For = pin },
            Given = ImplicitSources(specification.Given, pin),
            Then = ImplicitSources(specification.Then, pin),
            GeneratedValues = specification.GeneratedValues.Where(value => value.Property != identifier).ToList()
        };
    }

    static List<SpecificationStateModel> ImplicitSources(IEnumerable<SpecificationStateModel> states, LiteralSource pin) =>
        states.Select(state => Equals(state.For, pin) ? state with { For = null } : state).ToList();
}

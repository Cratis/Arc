// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Validation;

/// <summary>
/// Withholds opaque named rules that would prevent scenarios from executing declaratively.
/// </summary>
/// <param name="diagnostics">Where withheld rules are reported.</param>
public class ExecutableValidationRules(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Preserves scenarios by omitting named rules on their commands and carried concepts.
    /// </summary>
    /// <param name="model">The application, including scenarios and composite types.</param>
    /// <returns>The model with only admitted named rules.</returns>
    public ApplicationModel Apply(ApplicationModel model) => Apply(model, new HashSet<string>());

    /// <summary>
    /// Preserves generated-concept contracts even when opaque rules prevent scenario execution.
    /// </summary>
    /// <param name="model">The recovered application.</param>
    /// <param name="generatedConcepts">The concepts used for generation before admission pruning.</param>
    /// <param name="cap">The executable generation version boundary.</param>
    /// <returns>The model without weakening a generated concept's rules.</returns>
    internal ApplicationModel Apply(ApplicationModel model, IReadOnlySet<string> generatedConcepts, SemanticVersion? cap = null)
    {
        var exercised = model.Slices.SelectMany(slice => slice.Specifications)
            .Where(specification => specification.When is { Kind: SpecificationStateKind.Command } issued &&
                ResponseOnlyScenarios.CanBeStated(specification, model.Slices.SelectMany(slice => slice.Commands).FirstOrDefault(command => command.Name == issued.Name)))
            .Select(specification => specification.When!.Name).ToHashSet(StringComparer.Ordinal);
        var commands = model.Slices.SelectMany(slice => slice.Commands).Where(command => exercised.Contains(command.Name)).ToList();
        var owningConcepts = model.Concepts.Where(concept => OwnsValidation(concept, generatedConcepts, cap)).Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
        var opaqueConcepts = model.Concepts.Where(concept => owningConcepts.Contains(concept.Name) &&
            (concept.Validations.Any(rule => rule.Kind == ValidationRuleKind.Rule) ||
             (concept.Primitive == ScreenplayPrimitive.Uuid && concept.ValidatorSources.Count > 0 && (concept.Validations.Any() || !concept.ValidationRecoveryComplete))))
            .Select(concept => concept.Name).ToList();
        var unsupported = commands.Where(command => opaqueConcepts.Exists(concept => Carries(command, concept, model))).Select(command => command.Name).ToHashSet(StringComparer.Ordinal);

        return model with
        {
            Concepts = model.Concepts.Select(concept => owningConcepts.Contains(concept.Name) ? concept : concept with
            {
                Validations = Retain(concept.Validations, $"concept '{concept.Name}'", concept.Name, commands.Where(command => Carries(command, concept.Name, model)).Select(command => command.Name).ToList())
            }).ToList(),
            Slices = model.Slices.Select(slice => slice with
            {
                Specifications = slice.Specifications.Select(specification => specification.When is { } issued && unsupported.Contains(issued.Name)
                    ? specification with { GenerationFailure = "its generated concept has opaque validation that reference execution cannot evaluate; the validator was preserved" } : specification).ToList(),
                Commands = slice.Commands.Select(command => command with
                {
                    Validations = Retain(command.Validations, $"command '{command.Name}'", $"{slice.Namespace}.{command.Name}", exercised.Contains(command.Name) ? [command.Name] : [])
                }).ToList()
            }).ToList()
        };
    }

    static bool OwnsValidation(ConceptModel concept, IReadOnlySet<string> generatedConcepts, SemanticVersion? cap)
    {
        if (generatedConcepts.Contains(concept.Name))
        {
            return true;
        }
        if (concept.Generation is not { } generation)
        {
            return false;
        }

        return !generation.RequiresV21 || GenerationAdmission.Failure(concept, cap) is null;
    }

    static bool Carries(CommandModel command, string concept, ApplicationModel model)
    {
        var pending = new Queue<string>(command.Properties.Concat(command.Authoring?.Generated ?? []).Select(property => property.Type.Name)
            .Concat(command.Authoring?.UnavailableGenerated.Select(input => input.Type.Name) ?? []));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.TryDequeue(out var name))
        {
            if (name == concept)
            {
                return true;
            }
            if (!visited.Add(name))
            {
                continue;
            }
            foreach (var property in model.Types.Where(type => type.Name == name).SelectMany(type => type.Properties))
            {
                pending.Enqueue(property.Type.Name);
            }
        }

        return false;
    }

    List<ValidationRuleModel> Retain(IEnumerable<ValidationRuleModel> rules, string owner, string location, List<string> commands)
    {
        var retained = new List<ValidationRuleModel>();
        foreach (var rule in rules)
        {
            if (rule.Kind == ValidationRuleKind.Rule && commands.Count > 0)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableValidationRule,
                    $"The named rule '{rule.Value}' on {owner} was withheld to keep scenarios for command(s) '{string.Join(", ", commands.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}'; opaque rules cannot be evaluated by reference execution",
                    location);
            }
            else
            {
                retained.Add(rule);
            }
        }

        return retained;
    }
}

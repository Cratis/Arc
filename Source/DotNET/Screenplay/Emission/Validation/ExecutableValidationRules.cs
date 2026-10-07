// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Validation;

/// <summary>
/// Withholds opaque named rules that would prevent successful scenarios from executing declaratively.
/// </summary>
/// <param name="diagnostics">Where withheld rules are reported.</param>
public class ExecutableValidationRules(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Preserves successful scenarios by omitting named rules on their commands and carried concepts.
    /// </summary>
    /// <param name="model">The application, including scenarios and composite types.</param>
    /// <returns>The model with only admitted named rules.</returns>
    public ApplicationModel Apply(ApplicationModel model)
    {
        var successful = model.Slices.SelectMany(slice => slice.Specifications)
            .Where(specification => specification.When is { Kind: SpecificationStateKind.Command } && !specification.Errors.Any())
            .Select(specification => specification.When!.Name).ToHashSet(StringComparer.Ordinal);
        var commands = model.Slices.SelectMany(slice => slice.Commands).Where(command => successful.Contains(command.Name)).ToList();

        return model with
        {
            Concepts = model.Concepts.Select(concept => concept with
            {
                Validations = Retain(concept.Validations, $"concept '{concept.Name}'", concept.Name, commands.Where(command => Carries(command, concept.Name, model)).Select(command => command.Name).ToList())
            }).ToList(),
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Validations = Retain(command.Validations, $"command '{command.Name}'", $"{slice.Namespace}.{command.Name}", successful.Contains(command.Name) ? [command.Name] : [])
                }).ToList()
            }).ToList()
        };
    }

    static bool Carries(CommandModel command, string concept, ApplicationModel model)
    {
        var pending = new Queue<string>(command.Properties.Concat(command.Authoring?.Generated ?? []).Select(property => property.Type.Name));
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
                    $"The named rule '{rule.Value}' on {owner} was withheld to keep successful scenarios for command(s) '{string.Join(", ", commands.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}'; opaque rules cannot be evaluated by reference execution",
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

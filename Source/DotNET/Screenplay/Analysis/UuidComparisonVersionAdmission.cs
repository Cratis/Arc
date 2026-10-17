// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Keeps Uuid comparisons requiring ESM v28 out of documents targeting an earlier executable model.
/// </summary>
/// <param name="model">The recovered application.</param>
/// <param name="diagnostics">The diagnostic collector.</param>
internal sealed class UuidComparisonVersionAdmission(ApplicationModel model, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Withholds Uuid comparisons below their admitted version.
    /// </summary>
    /// <param name="model">The recovered application.</param>
    /// <param name="cap">The requested executable version cap.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <returns>The admitted application model.</returns>
    internal static ApplicationModel Apply(ApplicationModel model, SemanticVersion? cap, ScreenplayDiagnostics diagnostics)
    {
        if (cap is not { } version || version.IsAtLeast(SemanticVersion.V28))
        {
            return model;
        }
        var admission = new UuidComparisonVersionAdmission(model, diagnostics);

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => admission.Admit(command, slice.Namespace, version)).ToList()
            }).ToList()
        };
    }

    static string? TypeOf(string path, IEnumerable<PropertyModel> properties, IEnumerable<CommandReadModel> reads)
    {
        var split = path.Split('.', 2);

        return split.Length == 1
            ? properties.FirstOrDefault(property => property.Name == path)?.Type.Name
            : reads.FirstOrDefault(read => read.Alias == split[0])?.Properties.FirstOrDefault(property => property.Name == split[1])?.Type.Name;
    }

    CommandModel Admit(CommandModel command, string location, SemanticVersion cap)
    {
        var properties = command.Properties.Concat(command.Authoring?.Generated ?? []).ToList();
        var reads = command.Authoring?.Reads ?? [];
        var requirements = command.Authoring?.Requirements ?? [];
        var productions = command.Produces.Where(production => production.When is not { } when || !IsUuidCondition(when, properties, reads)).ToList();
        var kept = requirements.Where(requirement => !IsUuidCondition(requirement.Condition, properties, reads)).ToList();
        if (productions.Count == command.Produces.Count() && kept.Count == requirements.Count)
        {
            return command;
        }
        var compared = command.Produces.Except(productions).Where(production => production.When is not null).SelectMany(production => Paths(production.When!, properties, reads))
            .Concat(requirements.Except(kept).SelectMany(requirement => Paths(requirement.Condition, properties, reads))).Distinct(StringComparer.Ordinal);
        diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableValidationRule, $"Uuid comparisons on command '{command.Name}' ({string.Join(", ", compared)}) require ESM v28, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{cap}; the comparisons were left in code", location);

        return command with
        {
            Produces = productions,
            Authoring = command.Authoring is { } authoring ? authoring with { Requirements = kept } : null
        };
    }

    IEnumerable<string> Paths(ConditionModel condition, IEnumerable<PropertyModel> properties, IEnumerable<CommandReadModel> reads) => condition switch
    {
        ComparisonCondition comparison => new[] { comparison.Left, (comparison.Right as PropertyPathSource)?.Path }.OfType<string>().Where(path => IsUuid(path, properties, reads)),
        LogicalCondition logical => Paths(logical.Left, properties, reads).Concat(Paths(logical.Right, properties, reads)),
        _ => []
    };

    bool IsUuidCondition(ConditionModel condition, IEnumerable<PropertyModel> properties, IEnumerable<CommandReadModel> reads) => condition switch
    {
        ComparisonCondition comparison => Paths(comparison, properties, reads).Any(),
        LogicalCondition logical => IsUuidCondition(logical.Left, properties, reads) || IsUuidCondition(logical.Right, properties, reads),
        _ => false
    };

    bool IsUuid(string path, IEnumerable<PropertyModel> properties, IEnumerable<CommandReadModel> reads) => TypeOf(path, properties, reads) is { } name && IsUuidType(name);

    bool IsUuidType(string name) => string.Equals(name, "Uuid", StringComparison.Ordinal) || model.Concepts.Any(concept => concept.Name == name && concept.Primitive == ScreenplayPrimitive.Uuid);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Keeps comparisons requiring ESM v26 out of documents targeting an earlier executable model.
/// </summary>
/// <param name="model">The recovered application.</param>
/// <param name="cap">The requested executable version cap.</param>
/// <param name="diagnostics">The diagnostic collector.</param>
internal sealed class DateComparisonVersionAdmission(ApplicationModel model, SemanticVersion cap, ScreenplayDiagnostics diagnostics)
{
    static readonly ValidationRuleKind[] _comparisons =
    [
        ValidationRuleKind.Equal,
        ValidationRuleKind.NotEqual,
        ValidationRuleKind.GreaterThan,
        ValidationRuleKind.GreaterThanOrEqual,
        ValidationRuleKind.LessThan,
        ValidationRuleKind.LessThanOrEqual
    ];

    /// <summary>
    /// Withholds chronological comparisons below their admitted version.
    /// </summary>
    /// <param name="model">The recovered application.</param>
    /// <param name="cap">The requested executable version cap.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <returns>The admitted application model.</returns>
    internal static ApplicationModel Apply(ApplicationModel model, SemanticVersion? cap, ScreenplayDiagnostics diagnostics)
    {
        if (cap is not { } version || version.IsAtLeast(SemanticVersion.V26))
        {
            return model;
        }
        var admission = new DateComparisonVersionAdmission(model, version, diagnostics);

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => admission.Admit(command, slice.Namespace)).ToList()
            }).ToList()
        };
    }

    CommandModel Admit(CommandModel command, string location)
    {
        var properties = command.Properties.Concat(command.Authoring?.Generated ?? []).ToList();
        var requirements = command.Authoring?.Requirements ?? [];
        var validations = command.Validations.Where(rule => !IsDateRule(rule, properties)).ToList();
        var productions = command.Produces.Where(production => production.When is not { } when || !IsDateCondition(when, properties)).ToList();
        var kept = requirements.Where(requirement => !IsDateCondition(requirement.Condition, properties)).ToList();
        var withheld = command.Validations.Count() - validations.Count + command.Produces.Count() - productions.Count + requirements.Count - kept.Count;
        if (withheld == 0)
        {
            return command;
        }
        var dateProperties = ComparedDates(command.Validations.Except(validations), command.Produces.Except(productions), requirements.Except(kept), properties);
        diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableValidationRule, $"Date comparisons on command '{command.Name}' ({string.Join(", ", dateProperties)}) require ESM v26, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{cap}; the comparisons were left in code", location);

        return command with
        {
            Validations = validations,
            Produces = productions,
            Authoring = command.Authoring is { } authoring ? authoring with { Requirements = kept } : null
        };
    }

    IEnumerable<string> ComparedDates(IEnumerable<ValidationRuleModel> rules, IEnumerable<ProducesModel> productions, IEnumerable<CommandRequirementModel> requirements, IEnumerable<PropertyModel> properties) =>
        rules.Select(rule => rule.Property)
            .Concat(productions.Where(production => production.When is not null).SelectMany(production => DatePaths(production.When!, properties)))
            .Concat(requirements.SelectMany(requirement => DatePaths(requirement.Condition, properties)))
            .Distinct(StringComparer.Ordinal);

    IEnumerable<string> DatePaths(ConditionModel condition, IEnumerable<PropertyModel> properties) => condition switch
    {
        ComparisonCondition comparison => new[] { comparison.Left, (comparison.Right as PropertyPathSource)?.Path }.OfType<string>().Where(path => IsDate(path, properties)),
        LogicalCondition logical => DatePaths(logical.Left, properties).Concat(DatePaths(logical.Right, properties)),
        _ => []
    };

    bool IsDate(string path, IEnumerable<PropertyModel> properties) => properties.Any(property => property.Name == path && IsDateType(property.Type.Name));

    bool IsDateType(string name) => string.Equals(name, "Date", StringComparison.Ordinal) || string.Equals(name, "DateTime", StringComparison.Ordinal) || model.Concepts.Any(concept => concept.Name == name && concept.Primitive is ScreenplayPrimitive.Date or ScreenplayPrimitive.DateTime);

    bool IsDateRule(ValidationRuleModel rule, IEnumerable<PropertyModel> properties) => _comparisons.Contains(rule.Kind) && IsDate(rule.Property, properties);

    bool IsDateCondition(ConditionModel condition, IEnumerable<PropertyModel> properties) => condition switch
    {
        ComparisonCondition comparison => IsDate(comparison.Left, properties) || (comparison.Right is PropertyPathSource right && IsDate(right.Path, properties)),
        LogicalCondition logical => IsDateCondition(logical.Left, properties) || IsDateCondition(logical.Right, properties),
        _ => false
    };
}

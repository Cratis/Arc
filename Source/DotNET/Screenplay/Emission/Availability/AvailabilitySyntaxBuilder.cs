// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Availability;

/// <summary>
/// Builds the <c>available</c> gates of the artifacts and the <c>audience</c> and <c>availability</c> declarations they refer to.
/// </summary>
/// <remarks>
/// A gate and an availability line hold one form. A requirement that mixes forms with <c>and</c> is several lines, and one that mixes
/// them beneath an <c>or</c> or a <c>not</c> cannot be stated and is reported.
/// </remarks>
/// <param name="diagnostics">Where what cannot be stated is reported.</param>
public partial class AvailabilitySyntaxBuilder(ScreenplayDiagnostics diagnostics)
{
    const string UndeclaredDescription = "Defined in the host's AvailabilityCatalog; the rules are not recovered from code";

    readonly Dictionary<string, string?> _audiences = new(StringComparer.Ordinal);
    readonly Dictionary<string, AvailabilityBlockModel> _declared = new(StringComparer.Ordinal);
    readonly SortedSet<string> _referenced = new(StringComparer.Ordinal);
    readonly SortedSet<string> _opaque = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers the audiences and availabilities the code declares as types.
    /// </summary>
    /// <param name="audiences">The audiences.</param>
    /// <param name="availabilities">The availability blocks.</param>
    public void Declare(IEnumerable<AudienceModel> audiences, IEnumerable<AvailabilityBlockModel> availabilities)
    {
        foreach (var audience in audiences)
        {
            _audiences[audience.Name] = audience.Description;
        }

        foreach (var availability in availabilities)
        {
            _declared[availability.Name] = availability;
        }
    }

    /// <summary>
    /// Builds the gates an artifact carries.
    /// </summary>
    /// <param name="availability">The availability the artifact carries, if any.</param>
    /// <param name="location">Where the artifact lives, for use in diagnostics.</param>
    /// <returns>One <c>available</c> line for every gate that could be stated. Every one of them has to hold.</returns>
    public IEnumerable<AvailableSyntax> Build(AvailabilityModel? availability, string? location) =>
        Lines(availability?.Gates.Select(_ => _.Requirement) ?? [], location).Select(_ => new AvailableSyntax(_, SourceLocation.Start));

    /// <summary>
    /// Builds the gates a scope marker states.
    /// </summary>
    /// <param name="gates">The gates of the markers of one scope.</param>
    /// <param name="location">Where the markers are, for use in diagnostics.</param>
    /// <returns>One <c>available</c> line for every gate that could be stated.</returns>
    public IEnumerable<AvailableSyntax> Build(IEnumerable<AvailabilityGateModel> gates, string? location) =>
        Lines(gates.Select(_ => _.Requirement), location).Select(_ => new AvailableSyntax(_, SourceLocation.Start));

    /// <summary>
    /// Places the gates scope markers state on the modules, features and slices their namespaces name.
    /// </summary>
    /// <param name="modules">The modules built for the application.</param>
    /// <param name="gates">The gates of the markers.</param>
    /// <returns>The modules with the gates placed.</returns>
    public IReadOnlyList<ModuleSyntax> Place(IReadOnlyList<ModuleSyntax> modules, IEnumerable<ScopeGateModel> gates)
    {
        var placed = modules;
        foreach (var scope in gates.GroupBy(_ => string.Join('.', _.Scope), StringComparer.Ordinal))
        {
            var first = scope.First();
            var lines = Build(scope.Select(_ => _.Gate), first.Location).ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            var placement = new ScopePlacement(
                module => module with { Available = [.. module.Available, .. lines] },
                feature => feature with { Available = [.. feature.Available, .. lines] },
                slice => slice with { Available = [.. slice.Available, .. lines] });

            if (placement.Place(placed, first.Scope, true) is { } result)
            {
                placed = result;
                continue;
            }

            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"The scope marker in the namespace '{string.Join('.', first.Scope)}' names no module, feature or slice of the generated model, and its gate was left out",
                first.Location);
        }

        return placed;
    }

    /// <summary>
    /// Builds the audiences the code declares and the gates refer to.
    /// </summary>
    /// <returns>The <c>audience</c> declarations, ordered by name.</returns>
    public IEnumerable<AudienceSyntax> Audiences() =>
        _audiences.OrderBy(_ => _.Key, StringComparer.Ordinal).Select(_ => new AudienceSyntax(_.Key, SourceLocation.Start) { Description = _.Value });

    /// <summary>
    /// Builds the availabilities the code declares and the gates refer to.
    /// </summary>
    /// <param name="location">Where the application is, for use in diagnostics.</param>
    /// <returns>The <c>availability</c> declarations, ordered by name. One that is named and not declared as a type carries no rules.</returns>
    public IEnumerable<AvailabilitySyntax> Availabilities(string? location = null)
    {
        var declared = _declared.Values
            .Select(block => new AvailabilitySyntax(block.Name, [.. Lines(block.Lines, location)], SourceLocation.Start) { Description = block.Description });
        var undeclared = _referenced
            .Where(_ => !_declared.ContainsKey(_))
            .Select(_ => new AvailabilitySyntax(_, [], SourceLocation.Start) { Description = UndeclaredDescription });

        return declared.Concat(undeclared).OrderBy(_ => _.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds the application-wide values the opaque conditions read.
    /// </summary>
    /// <returns>One <c>provide</c> line for every condition class, decided by the host.</returns>
    public IEnumerable<ProvideSyntax> OpaqueConditions() =>
        _opaque.Select(_ => new ProvideSyntax(_, new TypeRefSyntax("Bool", false, false, SourceLocation.Start), new HostProvideSourceSyntax(SourceLocation.Start), SourceLocation.Start));

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Identifier();

    static ComparisonConditionSyntax Comparison(string left, ComparisonOperator op, object? value) =>
        new(left, op, new LiteralExpressionSyntax(Normalize(value), SourceLocation.Start), SourceLocation.Start);

    static object? Normalize(object? value) => value switch
    {
        sbyte or byte or short or ushort or int or uint => System.Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
        float or decimal => System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
        char character => character.ToString(),
        _ => value
    };

    static ConditionSyntax Join(IEnumerable<ConditionSyntax> parts, LogicalOperator op) =>
        parts.Aggregate((left, right) => new LogicalConditionSyntax(left, op, right, SourceLocation.Start));

    IEnumerable<AvailabilityRequirementSyntax> Lines(IEnumerable<AvailabilityRequirementModel> requirements, string? location)
    {
        foreach (var requirement in requirements)
        {
            if (requirement.Lines() is not { } lines)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                    $"An availability line mixes forms ({string.Join(", ", requirement.Forms().Select(_ => _.ToString().ToLowerInvariant()))}) beneath an or or a not, which a line cannot state, so it was left out; the part is not gated by it in the model",
                    location);
                continue;
            }

            foreach (var line in lines)
            {
                if (Requirement(line, location) is { } syntax)
                {
                    yield return syntax;
                }
            }
        }
    }

    AvailabilityRequirementSyntax? Requirement(AvailabilityRequirementModel requirement, string? location)
    {
        switch (requirement)
        {
            case AudienceRequirementModel audience:
                if (!Identifier().IsMatch(audience.Audience))
                {
                    diagnostics.Warning(
                        ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                        $"The gate [AvailableFor(\"{audience.Audience}\")] names an audience that is not an identifier and was left out; the part is not gated in the model",
                        location);
                    return null;
                }

                _audiences.TryAdd(audience.Audience, null);
                return new AudienceAvailabilityRequirementSyntax(audience.Audience, SourceLocation.Start) { Negated = audience.Negated };

            case ReferenceRequirementModel reference:
                return Reference(reference.Availability, location);

            case ConditionRequirementModel condition:
                return new ConditionAvailabilityRequirementSyntax(Condition(condition.Condition), SourceLocation.Start);

            case LogicalRequirementModel logical:
                var left = Requirement(logical.Left, location);
                var right = Requirement(logical.Right, location);
                return left is null || right is null
                    ? left ?? right
                    : new LogicalAvailabilityRequirementSyntax(left, logical.IsAnd ? LogicalOperator.And : LogicalOperator.Or, right, SourceLocation.Start);

            default:
                return null;
        }
    }

    AvailabilityReferenceSyntax? Reference(string name, string? location)
    {
        if (!Identifier().IsMatch(name))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"The gate [AvailableWith(\"{name}\")] names an availability that is not an identifier and was left out; the part is not gated in the model",
                location);
            return null;
        }

        if (_referenced.Add(name) && !_declared.ContainsKey(name))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityDefinitionNotRecovered,
                $"The availability '{name}' is defined in the host's AvailabilityCatalog, which is not read from code; the model declares it without rules, so complete them there, or define it as a type that derives from AvailabilityBlock",
                location);
        }

        return new AvailabilityReferenceSyntax(name, SourceLocation.Start);
    }

    ConditionSyntax Condition(AvailabilityConditionModel condition)
    {
        switch (condition)
        {
            case LogicalConditionModel logical:
                return new LogicalConditionSyntax(Condition(logical.Left), logical.IsAnd ? LogicalOperator.And : LogicalOperator.Or, Condition(logical.Right), SourceLocation.Start);

            case OpaqueConditionModel opaque:
                _opaque.Add(opaque.Name);
                return Comparison($"$context.{opaque.Name}", ComparisonOperator.Equal, true);

            case ComparisonConditionModel comparison:
                var left = $"$context.{comparison.Path}";
                var operands = comparison.Operands;
                return comparison.Operator switch
                {
                    "IsTrue" => Comparison(left, ComparisonOperator.Equal, true),
                    "Is" => Comparison(left, ComparisonOperator.Equal, operands[0]),
                    "IsNot" => Comparison(left, ComparisonOperator.NotEqual, operands[0]),
                    "IsGreaterThan" => Comparison(left, ComparisonOperator.GreaterThan, operands[0]),
                    "IsAtLeast" => Comparison(left, ComparisonOperator.GreaterThanOrEqual, operands[0]),
                    "IsLessThan" => Comparison(left, ComparisonOperator.LessThan, operands[0]),
                    "IsAtMost" => Comparison(left, ComparisonOperator.LessThanOrEqual, operands[0]),
                    "In" => Join(operands.Select(_ => (ConditionSyntax)Comparison(left, ComparisonOperator.Equal, _)), LogicalOperator.Or),
                    "NotIn" => Join(operands.Select(_ => (ConditionSyntax)Comparison(left, ComparisonOperator.NotEqual, _)), LogicalOperator.And),
                    _ => Comparison(left, ComparisonOperator.Equal, true)
                };

            default:
                throw new NotSupportedException($"Unsupported availability condition {condition.GetType().Name}.");
        }
    }
}

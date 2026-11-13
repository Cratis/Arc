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
/// <param name="diagnostics">Where what cannot be stated is reported.</param>
public partial class AvailabilitySyntaxBuilder(ScreenplayDiagnostics diagnostics)
{
    const string DefinitionDescription = "Defined in the host's AvailabilityCatalog; the rules are not recovered from code";

    readonly SortedSet<string> _audiences = new(StringComparer.Ordinal);
    readonly SortedSet<string> _availabilities = new(StringComparer.Ordinal);

    /// <summary>
    /// Builds the gates an artifact carries.
    /// </summary>
    /// <param name="availability">The availability the artifact carries, if any.</param>
    /// <param name="location">Where the artifact lives, for use in diagnostics.</param>
    /// <returns>One <c>available</c> line for every gate that could be stated. Every one of them has to hold.</returns>
    public IEnumerable<AvailableSyntax> Build(AvailabilityModel? availability, string? location)
    {
        var lines = new List<AvailableSyntax>();
        foreach (var gate in availability?.Gates ?? [])
        {
            var requirement = gate.Availability is null ? Audiences(gate, location) : Reference(gate.Availability, location);
            if (requirement is not null)
            {
                lines.Add(new(requirement, SourceLocation.Start));
            }
        }

        return lines;
    }

    /// <summary>
    /// Builds the audiences the gates refer to.
    /// </summary>
    /// <returns>The <c>audience</c> declarations, ordered by name.</returns>
    public IEnumerable<AudienceSyntax> Audiences() => _audiences.Select(_ => new AudienceSyntax(_, SourceLocation.Start));

    /// <summary>
    /// Builds the availabilities the gates refer to.
    /// </summary>
    /// <returns>The <c>availability</c> declarations, ordered by name. They carry no rules: those are in the host.</returns>
    public IEnumerable<AvailabilitySyntax> Availabilities() =>
        _availabilities.Select(_ => new AvailabilitySyntax(_, [], SourceLocation.Start) { Description = DefinitionDescription });

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Identifier();

    AvailabilityRequirementSyntax? Audiences(AvailabilityGateModel gate, string? location)
    {
        var names = gate.Audiences.Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 0 || names.Exists(_ => !Identifier().IsMatch(_)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"The gate [AvailableFor({string.Join(", ", names.Select(_ => $"\"{_}\""))})] names an audience that is not an identifier and was left out; the part is not gated in the model",
                location);
            return null;
        }

        foreach (var name in names)
        {
            _audiences.Add(name);
        }

        return names
            .Select(_ => (AvailabilityRequirementSyntax)new AudienceAvailabilityRequirementSyntax(_, SourceLocation.Start))
            .Aggregate((left, right) => new LogicalAvailabilityRequirementSyntax(left, LogicalOperator.Or, right, SourceLocation.Start));
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

        if (_availabilities.Add(name))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityDefinitionNotRecovered,
                $"The availability '{name}' is defined in the host's AvailabilityCatalog, which is not read from code; the model declares it without rules, so complete them there",
                location);
        }

        return new AvailabilityReferenceSyntax(name, SourceLocation.Start);
    }
}

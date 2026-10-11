// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Tracks member-level personal-data classification and the concept-wide meaning it acquires in Screenplay.
/// </summary>
public class PersonalDataMarkings
{
    readonly HashSet<string> _nonConceptMembers = new(StringComparer.Ordinal);
    readonly HashSet<string> _promoted = new(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<string>> _unmarked = new(StringComparer.Ordinal);

    /// <summary>
    /// Records one analyzed member's classification.
    /// </summary>
    /// <param name="property">The property, including its positional constructor parameter markings.</param>
    /// <returns>Whether the member marks a concept as personal data.</returns>
    public bool Record(IPropertySymbol property)
    {
        var carried = UnderlyingTypes.Of(property.Type);
        var marked = MemberAttributes.Has(property, WellKnownTypeNames.PiiAttribute);
        var member = $"{property.ContainingType.ToDisplayString()}.{property.Name}";
        if (carried.TypeKind != TypeKind.Enum && carried.FindBase(WellKnownTypeNames.ConceptAs) is null)
        {
            if (marked)
            {
                _nonConceptMembers.Add(member);
            }

            return false;
        }

        if (carried.HasAttribute(WellKnownTypeNames.PiiAttribute))
        {
            return marked;
        }

        if (marked)
        {
            _promoted.Add(carried.Name);
        }
        else
        {
            if (!_unmarked.TryGetValue(carried.Name, out var uses))
            {
                uses = new(StringComparer.Ordinal);
                _unmarked[carried.Name] = uses;
            }

            uses.Add(member);
        }

        return marked;
    }

    /// <summary>
    /// Reports classification the document cannot state faithfully.
    /// </summary>
    /// <param name="diagnostics">Where the loss or promotion is reported.</param>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        foreach (var member in _nonConceptMembers.Order(StringComparer.Ordinal))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.MemberPiiNotRepresentable,
                $"Member '{member}' has [PII] on a non-concept type; Screenplay classifies only concepts, so the marking is not stated. Use a personal-data concept to preserve the classification",
                member);
        }

        foreach (var concept in _promoted.Where(_unmarked.ContainsKey).Order(StringComparer.Ordinal))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ConceptPiiPromotion,
                $"Concept '{concept}' is promoted to pii by member [PII] markings; the document also classifies these unmarked uses as pii: {string.Join(", ", _unmarked[concept].Order(StringComparer.Ordinal))}",
                concept);
        }
    }
}

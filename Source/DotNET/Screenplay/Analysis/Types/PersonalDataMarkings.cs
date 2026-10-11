// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Tracks member-level personal-data classification and the concept-wide meaning it acquires in Screenplay.
/// </summary>
internal class PersonalDataMarkings
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
            RecordNonConceptMember(member, marked);

            return false;
        }

        if (carried.HasAttribute(WellKnownTypeNames.PiiAttribute))
        {
            return marked;
        }

        if (marked)
        {
            _promoted.Add(carried.Name);

            return true;
        }

        RecordUnmarkedUse(carried.Name, member);

        return false;
    }

    /// <summary>
    /// Reports member classification the document cannot state.
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
    }

    /// <summary>
    /// Gets the unmarked uses affected if a promoted concept is emitted as pii.
    /// </summary>
    /// <param name="concept">The concept name.</param>
    /// <returns>The analyzed uses, without duplicates and in stable order.</returns>
    public IReadOnlyList<string> UnmarkedUses(string concept) =>
        _promoted.Contains(concept) && _unmarked.TryGetValue(concept, out var uses) ? [.. uses.Order(StringComparer.Ordinal)] : [];

    void RecordNonConceptMember(string member, bool marked)
    {
        if (marked)
        {
            _nonConceptMembers.Add(member);
        }
    }

    void RecordUnmarkedUse(string concept, string member)
    {
        if (!_unmarked.TryGetValue(concept, out var uses))
        {
            uses = new(StringComparer.Ordinal);
            _unmarked[concept] = uses;
        }

        uses.Add(member);
    }
}

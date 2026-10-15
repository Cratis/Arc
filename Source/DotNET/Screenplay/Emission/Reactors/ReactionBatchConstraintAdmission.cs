// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Admits atomic batches only while all their rejecting constraints remain declaratively represented.
/// </summary>
internal static class ReactionBatchConstraintAdmission
{
    /// <summary>
    /// Finds a required constraint absent from the document's declarations.
    /// </summary>
    /// <param name="gap">The analyzed batch and its required constraints.</param>
    /// <param name="application">The model being emitted.</param>
    /// <returns>A missing-constraint reason, or null when every constraint is represented.</returns>
    internal static ReactionBatchGap? Missing(ReactionBatchGap gap, ApplicationModel application)
    {
        var declared = application.Slices.SelectMany(slice => slice.Constraints).ToList();
        var missing = gap.RequiredConstraints.FirstOrDefault(required => !declared.Exists(actual => Equivalent(required, actual)));

        return missing is null ? null : new(ReactionBatchGapKind.Unrepresented, $"constraint '{missing.Name}' is not represented in the document");
    }

    static bool Equivalent(ConstraintModel required, ConstraintModel actual) =>
        required.Name == actual.Name && SameRule(required, actual) &&
        required.ReleasedBy.SequenceEqual(actual.ReleasedBy, StringComparer.Ordinal) &&
        required.AdditionalRules.Count() == actual.AdditionalRules.Count() &&
        required.AdditionalRules.Zip(actual.AdditionalRules).All(pair => Equivalent(pair.First, pair.Second));

    static bool SameRule(ConstraintModel required, ConstraintModel actual) => (required, actual) switch
    {
        (UniqueEventConstraintModel expected, UniqueEventConstraintModel declared) => expected.EventName == declared.EventName,
        (UniquePropertyConstraintModel expected, UniquePropertyConstraintModel declared) =>
            expected.EventName == declared.EventName && expected.Property == declared.Property && expected.IgnoreCasing == declared.IgnoreCasing &&
            expected.AdditionalProperties.SequenceEqual(declared.AdditionalProperties, StringComparer.Ordinal),
        _ => false
    };
}

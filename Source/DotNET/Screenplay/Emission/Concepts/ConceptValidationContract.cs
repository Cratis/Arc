// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Concepts;

/// <summary>
/// Shares the effective validation rules and mandatory preservation decision at an executable version cap.
/// </summary>
internal sealed record ConceptValidationContract
{
    /// <summary>
    /// Gets the exact rules the concept declaration will state.
    /// </summary>
    internal required IReadOnlyList<ValidationRuleModel> Rules { get; init; }

    /// <summary>
    /// Gets the cap refusal requiring preservation, or null for the ordinary contract.
    /// </summary>
    internal string? VersionFailure { get; init; }

    /// <summary>
    /// Gets whether rules must stay intact even when that prevents scenario execution.
    /// </summary>
    internal bool MustPreserve => VersionFailure is not null;

    /// <summary>
    /// Gets whether this preserved contract cannot be evaluated by the reference runner.
    /// </summary>
    internal bool BlocksExecution => MustPreserve || Rules.Any(rule => rule.Kind == ValidationRuleKind.Rule);

    /// <summary>
    /// Selects the same validation contract for declaration and scenario admission.
    /// </summary>
    /// <param name="concept">The recovered concept and complete source validators.</param>
    /// <param name="cap">The requested executable version boundary.</param>
    /// <returns>The effective contract, including any non-executable cap fallback.</returns>
    internal static ConceptValidationContract For(ConceptModel concept, SemanticVersion? cap) => IsChecksumCapped(concept, cap)
        ? CappedChecksum(concept, cap) : new() { Rules = [.. UuidRules(concept)] };

    static bool IsChecksumCapped(ConceptModel concept, SemanticVersion? cap) => cap is { } version &&
        !version.IsAtLeast(SemanticVersion.V21) && concept.Validations.Any(rule => rule.Kind == ValidationRuleKind.Checksum);

    static ConceptValidationContract CappedChecksum(ConceptModel concept, SemanticVersion? cap)
    {
        var retained = concept.ValidatorSources.Count > 0 ? "the original validator is retained" : "the validator remains in code; no portable source attachment is available";
        var reason = $"checksum validation requires ESM v21, but the cap is ESM v{cap}; {retained}";
        var rules = concept.ValidatorSources.Concat(concept.Validations.Where(rule => rule.Kind != ValidationRuleKind.Checksum));

        return new() { Rules = [.. rules], VersionFailure = reason };
    }

    static IEnumerable<ValidationRuleModel> UuidRules(ConceptModel concept) => NeedsUuidSource(concept) ? concept.ValidatorSources : concept.Validations;

    static bool NeedsUuidSource(ConceptModel concept) => concept.Primitive == ScreenplayPrimitive.Uuid && UsesGeneration(concept) && HasValidatorSource(concept);

    static bool UsesGeneration(ConceptModel concept) => concept.HasGeneratedUses || concept.Generation is not null;

    static bool HasValidatorSource(ConceptModel concept) => concept.ValidatorSources.Count > 0 && (concept.Validations.Any() || !concept.ValidationRecoveryComplete);
}

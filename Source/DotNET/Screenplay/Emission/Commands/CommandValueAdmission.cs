// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Checks generation version boundaries and reports withheld command values.
/// </summary>
/// <param name="diagnostics">Where unadmitted values are reported.</param>
internal class CommandValueAdmission(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Checks whether the cap requires legacy generation and response syntax.
    /// </summary>
    /// <param name="authoring">The recovered command values.</param>
    /// <param name="maximumVersion">The executable version cap.</param>
    /// <returns>Whether v7 command values must be withheld.</returns>
    public static bool RequiresLegacyValues(CommandAuthoringModel authoring, SemanticVersion? maximumVersion) =>
        IsBelow(maximumVersion, SemanticVersion.V7) && HasCommandValues(authoring);

    /// <summary>
    /// Checks whether authoring-only values may be retained without executable admission.
    /// </summary>
    /// <param name="authoring">The recovered command values.</param>
    /// <param name="model">The application's concepts.</param>
    /// <param name="maximumVersion">The executable version cap.</param>
    /// <param name="authoringOnlyConstructs">Whether authoring-only syntax is enabled.</param>
    /// <returns>Whether neither version boundary withholds authoring values.</returns>
    public static bool KeepsAuthoring(CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? maximumVersion, bool authoringOnlyConstructs) =>
        authoringOnlyConstructs && !RequiresLegacyValues(authoring, maximumVersion) && !WithholdsStrategy(authoring, model, maximumVersion);

    /// <summary>
    /// Collects generated names outside the executable contract and reports their omission.
    /// </summary>
    /// <param name="command">The command with recovered generation.</param>
    /// <param name="model">The application's concepts.</param>
    /// <param name="location">The command's enclosing diagnostic location.</param>
    /// <param name="maximumVersion">The executable version cap.</param>
    /// <returns>The blocked generated property names.</returns>
    public HashSet<string> Blocked(CommandModel command, ApplicationModel model, string location, SemanticVersion? maximumVersion)
    {
        var authoring = command.Authoring!;
        var failures = authoring.Generated.ToDictionary(property => property.Name, property => GeneratedConceptAdmission.Failure(property, authoring, model, maximumVersion), StringComparer.Ordinal);
        var blocked = failures.Where(failure => failure.Value is not null).Select(failure => failure.Key).ToHashSet(StringComparer.Ordinal);
        ReportImplicitContracts(authoring, model, blocked, maximumVersion);
        ReportIdentifiers(authoring, model, failures, location);
        ReportBlocked(command, blocked, location, maximumVersion);

        return blocked;
    }

    static bool IsBelow(SemanticVersion? maximumVersion, SemanticVersion required) => maximumVersion is { } cap && !cap.IsAtLeast(required);

    static bool HasCommandValues(CommandAuthoringModel authoring) => authoring.Generated.Count > 0 || authoring.Response is not null || authoring.ResponseFields.Count > 0;

    static bool WithholdsStrategy(CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? maximumVersion) =>
        authoring.Generated.Any(property => !GeneratedConceptAdmission.Allows(property, authoring, model, maximumVersion));

    void ReportImplicitContracts(CommandAuthoringModel authoring, ApplicationModel model, HashSet<string> blocked, SemanticVersion? cap)
    {
        var blockedConcepts = authoring.Generated.Where(property => blocked.Contains(property.Name)).Select(property => property.Type.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var (concept, reason) in model.Concepts.Where(concept => concept.Generation is null && blockedConcepts.Contains(concept.Name))
            .Select(concept => (Concept: concept, Reason: GenerationAdmission.Failure(concept, cap))).Where(result => result.Reason is not null))
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.GenerationNotRecoverable, $"Concept '{concept.Name}': {reason}; its generation contract remains in code", concept.GenerationLocation ?? concept.Name);
        }
    }

    void ReportIdentifiers(CommandAuthoringModel authoring, ApplicationModel model, Dictionary<string, string?> failures, string location)
    {
        foreach (var property in authoring.Generated.Where(property => property.Name == authoring.Identifier && failures[property.Name] is not null))
        {
            var concept = model.Concepts.SingleOrDefault(concept => concept.Name == property.Type.Name);
            if (concept?.Generation?.RequiresV21 == true)
            {
                diagnostics.Information(ScreenplayDiagnosticCodes.GenerationNotRecoverable, $"Generated identifier '{property.Name}': {failures[property.Name]}; generation remains in code", concept.GenerationLocation ?? location);
            }
        }
    }

    void ReportBlocked(CommandModel command, HashSet<string> blocked, string location, SemanticVersion? cap)
    {
        if (blocked.Count == 0)
        {
            return;
        }
        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnreadableCommandResponse,
            $"Generated values '{string.Join(", ", blocked.Order(StringComparer.Ordinal))}' must be required scalar generatable concepts with completely recovered, non-opaque, non-refuted concept validation; non-default strategies require ESM v15 and concept rules require ESM v20; pattern, random, rule and checksum require ESM v21 (cap v{cap?.ToString() ?? "latest"}); compliance markers and generated identifiers below 64 random bits remain refused (PLAY0268/PLAY0684); those values, dependent responses, mappings, and production conditions were left in code",
            $"{location}.{command.Name}");
    }
}

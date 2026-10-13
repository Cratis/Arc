// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        var blocked = authoring.Generated.Where(property => !GeneratedConceptAdmission.Allows(property, authoring, model, maximumVersion)).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        if (blocked.Count > 0)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnreadableCommandResponse,
                $"Generated values '{string.Join(", ", blocked.Order(StringComparer.Ordinal))}' must be required scalar generatable concepts with no concept validators, validation rules or compliance markers (PLAY0268); non-default strategies require ESM v15 (cap v{maximumVersion?.ToString() ?? "latest"}) and generated identifiers require at least 64 random bits (PLAY0684); those values, dependent responses, mappings, and production conditions were left in code",
                $"{location}.{command.Name}");
        }

        return blocked;
    }

    static bool IsBelow(SemanticVersion? maximumVersion, SemanticVersion required) => maximumVersion is { } cap && !cap.IsAtLeast(required);

    static bool HasCommandValues(CommandAuthoringModel authoring) => authoring.Generated.Count > 0 || authoring.Response is not null || authoring.ResponseFields.Count > 0;

    static bool WithholdsStrategy(CommandAuthoringModel authoring, ApplicationModel model, SemanticVersion? maximumVersion) =>
        IsBelow(maximumVersion, SemanticVersion.V15) && authoring.Generated.Any(property => HasStrategy(property.Type.Name, model));

    static bool HasStrategy(string name, ApplicationModel model) => model.Concepts.Any(concept => concept.Name == name && concept.Generation is { Kind: not "uuid" });
}

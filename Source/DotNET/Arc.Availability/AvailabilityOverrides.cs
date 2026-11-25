// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents what a developer has switched on or off to see how the application behaves.
/// </summary>
/// <param name="EnabledAudiences">Audiences every caller is treated as belonging to.</param>
/// <param name="DisabledAudiences">Audiences no caller is treated as belonging to. Wins over <paramref name="EnabledAudiences"/>.</param>
/// <param name="EnabledPaths">Module, feature or slice paths, separated by '/', that are available whatever their gates say.</param>
/// <param name="DisabledPaths">Module, feature or slice paths, separated by '/', that are not available. Wins over <paramref name="EnabledPaths"/>.</param>
public record AvailabilityOverrides(
    IReadOnlyList<string> EnabledAudiences,
    IReadOnlyList<string> DisabledAudiences,
    IReadOnlyList<string> EnabledPaths,
    IReadOnlyList<string> DisabledPaths)
{
    /// <summary>
    /// Gets overrides that change nothing.
    /// </summary>
    public static AvailabilityOverrides None { get; } = new([], [], [], []);

    /// <summary>
    /// Gets a value indicating whether nothing is overridden.
    /// </summary>
    public bool IsEmpty => EnabledAudiences.Count == 0 && DisabledAudiences.Count == 0 && EnabledPaths.Count == 0 && DisabledPaths.Count == 0;

    /// <summary>
    /// Creates overrides with blank and duplicate entries removed and every list in a stable order.
    /// </summary>
    /// <param name="overrides">The overrides to normalize.</param>
    /// <returns>The normalized <see cref="AvailabilityOverrides"/>.</returns>
    public static AvailabilityOverrides Normalize(AvailabilityOverrides? overrides) =>
        overrides is null
            ? None
            : new(Clean(overrides.EnabledAudiences), Clean(overrides.DisabledAudiences), Clean(overrides.EnabledPaths), Clean(overrides.DisabledPaths));

    static string[] Clean(IReadOnlyList<string>? values) =>
        [.. (values ?? []).Where(_ => !string.IsNullOrWhiteSpace(_)).Select(_ => _.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
}

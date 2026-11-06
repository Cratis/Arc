// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Explicit receiving resource and environment. Never inferred from Host, Origin, or forwarded headers.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="Resource">The configured canonical resource URI.</param>
/// <param name="Environment">The configured environment.</param>
internal sealed record VerifiedCallerInstallation(string? Resource, string? Environment)
{
    /// <summary>
    /// Checks whether the installation names a resource, environment, and unambiguous receiving audience.
    /// </summary>
    /// <remarks>Mirrors the verified-caller builder's installation check.</remarks>
    /// <param name="catalog">The configured resource catalog.</param>
    /// <returns>Whether the installation can receive verified callers.</returns>
    internal bool IsConfigured(CallerResourceCatalog catalog) =>
        !string.IsNullOrWhiteSpace(Resource) &&
        !string.IsNullOrWhiteSpace(Environment) &&
        catalog.TryGetAudience(Resource, out var audience) &&
        !string.IsNullOrWhiteSpace(audience);
}

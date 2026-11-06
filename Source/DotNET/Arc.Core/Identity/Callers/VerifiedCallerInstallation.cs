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
    /// <param name="catalog">The configured resource catalog.</param>
    /// <returns>Whether the installation can receive verified callers.</returns>
    internal bool IsConfigured(CallerResourceCatalog catalog) =>
        Resource is not null && catalog.TryGetAudience(Resource, out var audience) && IsValid(Resource, audience, Environment);

    /// <summary>Checks the shared installation binding requirements.</summary>
    /// <param name="resource">The receiving resource.</param>
    /// <param name="audience">The receiving audience.</param>
    /// <param name="environment">The receiving environment.</param>
    /// <returns>Whether all binding values are present.</returns>
    internal static bool IsValid(string? resource, string? audience, string? environment) =>
        !string.IsNullOrWhiteSpace(resource) && !string.IsNullOrWhiteSpace(audience) && !string.IsNullOrWhiteSpace(environment);
}

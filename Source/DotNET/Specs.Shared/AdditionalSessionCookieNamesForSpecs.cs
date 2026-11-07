// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A test-only proxy base for the host's internal session-cookie-name seam.
/// </summary>
public class AdditionalSessionCookieNamesForSpecs : IAdditionalSessionCookieNames
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> Names => ConfiguredNames();

    /// <summary>Supplies the additional cookie names configured by a spec.</summary>
    /// <returns>The additional session-cookie names.</returns>
    public virtual IReadOnlyCollection<string> ConfiguredNames() => [];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Carries a proven caller or the reason the setup could not be recovered.
/// </summary>
/// <param name="Caller">The caller, or null.</param>
/// <param name="Reason">The unsupported-setup reason, or null when no setup was attempted.</param>
internal sealed record SpecificationCallerRecovery(SpecificationCallerModel? Caller, string? Reason)
{
    /// <summary>
    /// Records an unsupported caller setup.
    /// </summary>
    /// <param name="reason">The precise reason.</param>
    /// <returns>The failed recovery.</returns>
    internal static SpecificationCallerRecovery Failed(string reason) => new(null, reason);
}

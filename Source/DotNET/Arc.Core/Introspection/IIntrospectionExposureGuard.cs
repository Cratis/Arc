// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Checks whether the host can enforce authentication on the discovery endpoints before they are mapped.
/// </summary>
internal interface IIntrospectionExposureGuard
{
    /// <summary>
    /// Finds the reason the host cannot require authenticated callers, if any.
    /// </summary>
    /// <returns>The reason, or null if the host can enforce authentication.</returns>
    string? FindEnforcementProblem();
}

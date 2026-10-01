// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Checks whether the host can enforce authentication on the discovery endpoints before they are mapped.
/// </summary>
internal interface IIntrospectionExposureGuard
{
    /// <summary>
    /// Gets the host services, when available, for resolving the actual host environment.
    /// </summary>
    IServiceProvider? Services => null;

    /// <summary>
    /// Finds the reason the host cannot require authenticated callers, if any.
    /// </summary>
    /// <param name="services">The services used to resolve discovery exposure.</param>
    /// <returns>The reason, or null if the host can enforce authentication.</returns>
    string? FindEnforcementProblem(IServiceProvider? services);

    /// <summary>
    /// Defers discovery mapping until the host services are available, if necessary.
    /// </summary>
    /// <param name="mapping">The mapping to perform with the actual host services.</param>
    /// <returns>Whether mapping was deferred.</returns>
    bool TryDeferMapping(Action<IServiceProvider> mapping) => false;
}

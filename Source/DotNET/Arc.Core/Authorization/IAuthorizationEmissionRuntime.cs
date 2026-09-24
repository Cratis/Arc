// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Isolates native host context during observable query emissions, including after the admission request is gone.
/// </summary>
public interface IAuthorizationEmissionRuntime
{
    /// <summary>
    /// Begins a native operation-local context. Hub subscriptions suppress native HTTP after admission;
    /// direct streams may use their still-live request context.
    /// </summary>
    /// <param name="principal">The subscriber principal.</param>
    /// <param name="services">The emission provider.</param>
    /// <param name="hasLiveRequest">Whether the originating HTTP request is still alive.</param>
    /// <returns>A scope restoring the producer's native context.</returns>
    IDisposable? BeginEmissionScope(ClaimsPrincipal principal, IServiceProvider services, bool hasLiveRequest);

    /// <summary>
    /// Captures only a still-live direct request so later emissions can build isolated native HTTP facades.
    /// The returned delegate must not be retained after that request completes.
    /// </summary>
    /// <param name="services">The admitting request provider.</param>
    /// <returns>An operation-local scope factory, or null when no native request is available.</returns>
    Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? CaptureLiveRequest(IServiceProvider services);
}

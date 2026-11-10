// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Endpoint metadata that lets one exact route stay reachable without a verified caller when
/// <see cref="CapabilitiesOptions.GatedEndpoints"/> is unset and the whole port is gated, such as an OAuth discovery
/// document a client must read before it can authenticate.
/// </summary>
/// <remarks>
/// Internal on purpose: only Arc's own hosting packages, through <c>InternalsVisibleTo</c>, can issue it, so application
/// code cannot mark an endpoint or route group as ungated. The exemption is also bound to the route: the endpoint must
/// be a <c>GET</c>-only endpoint whose route pattern is exactly <see cref="Path"/>. Anything else carrying the metadata,
/// such as a route group's metadata inherited by other endpoints, stays gated. With an explicit
/// <see cref="CapabilitiesOptions.GatedEndpoints"/> selection it has no effect.
/// </remarks>
/// <param name="path">The exact route pattern, as mapped, that is exempt.</param>
internal sealed class CapabilityGateExemption(string path)
{
    /// <summary>
    /// Gets the exact route pattern that is exempt.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Tests whether an endpoint is the one exact route exempted from the whole-port gate. Fails closed.
    /// </summary>
    /// <param name="endpoint">The route endpoint.</param>
    /// <returns>True only for a GET-only endpoint on the exempt route pattern that carries the exemption.</returns>
    public static bool Exempts(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<CapabilityGateExemption>() is { } exemption &&
        !string.IsNullOrEmpty(exemption.Path) &&
        string.Equals(endpoint.RoutePattern.RawText, exemption.Path, StringComparison.Ordinal) &&
        endpoint.Metadata.GetMetadata<HttpMethodMetadata>() is { HttpMethods: [var method] } &&
        string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);
}

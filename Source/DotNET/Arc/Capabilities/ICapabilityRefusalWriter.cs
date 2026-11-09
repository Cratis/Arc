// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Writes the response for a request the capability gate refuses.
/// </summary>
/// <remarks>
/// The gate looks for an implementation in the metadata of the endpoint the request was routed to. An endpoint that
/// speaks a protocol with its own challenge format, such as MCP, adds a writer to its metadata. Every other endpoint,
/// and every request whose endpoint is not known when the gate runs, keeps the default problem-details response.
/// A writer only shapes the refusal: the request is refused whatever the writer does, and the endpoint never runs.
/// </remarks>
public interface ICapabilityRefusalWriter
{
    /// <summary>
    /// Writes the refusal to the response.
    /// </summary>
    /// <param name="context">The refused request.</param>
    /// <param name="refusal">Why the request was refused.</param>
    /// <returns>Awaitable task.</returns>
    Task Write(HttpContext context, CapabilityRefusal refusal);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the verified caller of an MCP request, as admitted by the capability gate.
/// </summary>
/// <param name="Capability">The caller projection the gate stored on the request.</param>
/// <param name="Verified">The verified caller behind the projection.</param>
internal sealed record McpCaller(CapabilityCaller Capability, VerifiedCaller Verified)
{
    /// <summary>
    /// Gets whether the caller presented an OAuth grant, so a missing scope can be stepped up through OAuth.
    /// </summary>
    internal bool CanStepUp => Verified.CredentialKind == CallerCredentialKind.OAuthGrant;

    /// <summary>
    /// Reads the caller the gate admitted for a request.
    /// </summary>
    /// <param name="context">The request, or null outside a request.</param>
    /// <returns>The caller, or null when the request carries no admitted caller.</returns>
    /// <remarks>
    /// The caller comes only from the items the gate stored. The request body, headers and tool arguments are never read.
    /// </remarks>
    internal static McpCaller? From(HttpContext? context)
    {
        if (context?.Items.TryGetValue(CapabilityCallers.ItemKey, out var item) != true || item is not CapabilityCaller capability)
        {
            return null;
        }

        var verified = VerifiedCallerPrincipal.From(context.Items)?.Identities.OfType<VerifiedCallerIdentity>().FirstOrDefault()?.Caller;
        return verified is null ? null : new McpCaller(capability, verified);
    }

    /// <summary>
    /// Determines whether the caller's scopes satisfy a tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <returns>True when the caller holds the required scopes or one complete alternative set.</returns>
    internal bool HoldsScopesFor(McpToolDescriptor tool) => tool.IsSatisfiedBy(Verified.Scopes);
}

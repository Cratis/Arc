// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities;

/// <summary>Provides transport access to facts already admitted by the capability gate.</summary>
public static class CapabilityRequestContext
{
    /// <summary>Reads the admitted caller, without trusting the request's user principal.</summary>
    /// <param name="items">The current request's item bag.</param>
    /// <returns>The admitted capability caller, or null.</returns>
    public static CapabilityCaller? GetCaller(IDictionary<object, object?> items) =>
        items.TryGetValue(CapabilityCallers.ItemKey, out var caller) ? caller as CapabilityCaller : null;

    /// <summary>Reads the gate's verified identity without permitting transports to create one.</summary>
    /// <param name="items">The current request's item bag.</param>
    /// <returns>The verified caller, or null.</returns>
    public static VerifiedCaller? GetVerifiedCaller(IDictionary<object, object?> items) =>
        VerifiedCallerPrincipal.From(items)?.Identities.OfType<VerifiedCallerIdentity>().FirstOrDefault()?.Caller;

    /// <summary>Labels an already admitted caller with its transport protocol; grants no authority.</summary>
    /// <param name="items">The current request's item bag.</param>
    /// <param name="protocol">The transport protocol.</param>
    public static void SetProtocol(IDictionary<object, object?> items, string protocol)
    {
        if (GetCaller(items) is { } caller)
        {
            items[CapabilityCallers.ItemKey] = caller with { Protocol = protocol };
        }
    }
}

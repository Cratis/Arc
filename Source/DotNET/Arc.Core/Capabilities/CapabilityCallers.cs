// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Capabilities;

/// <summary>Reads the capability caller from the current request.</summary>
/// <param name="requests">The current request accessor.</param>
internal sealed class CapabilityCallers(IHttpRequestContextAccessor requests) : ICapabilityCallers
{
    /// <summary>The request item holding the admitted caller.</summary>
    internal static readonly object ItemKey = new();

    /// <inheritdoc/>
    public CapabilityCaller? Current =>
        requests.Current is { } request && request.Items.TryGetValue(ItemKey, out var caller)
            ? caller as CapabilityCaller
            : null;
}

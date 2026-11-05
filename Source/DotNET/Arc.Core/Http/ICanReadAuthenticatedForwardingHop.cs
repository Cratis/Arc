// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>Exposes host-validated evidence about the immediate TLS peer, never forwarded transport headers.</summary>
public interface ICanReadAuthenticatedForwardingHop
{
    /// <summary>Gets whether the immediate peer's client certificate passed the configured forwarding-hop policy.</summary>
    bool IsAuthenticatedForwardingHop { get; }
}

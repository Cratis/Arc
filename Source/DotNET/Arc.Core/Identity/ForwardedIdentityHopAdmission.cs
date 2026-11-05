// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Describes admission of the immediate forwarding peer.
/// </summary>
internal enum ForwardedIdentityHopAdmission
{
    /// <summary>The host does not require admission.</summary>
    NotRequired = 0,

    /// <summary>The immediate TLS peer is admitted.</summary>
    Authenticated = 1,

    /// <summary>The host cannot expose its TLS transport.</summary>
    NoTransportCertificate = 2,

    /// <summary>The transport supplied no client certificate.</summary>
    NoClientCertificate = 3,

    /// <summary>The certificate does not chain to the private authorities.</summary>
    UntrustedCertificate = 4,

    /// <summary>The certificate has no admitted DNS SAN.</summary>
    UnexpectedIdentity = 5
}

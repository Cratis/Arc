// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Arc.Http;

/// <summary>
/// Exposes only the peer certificate from TLS terminated by the host itself.
/// </summary>
internal interface ICanReadClientCertificate
{
    /// <summary>
    /// Gets a copy of the transport certificate, owned by the caller, or null when none was presented.
    /// </summary>
    X509Certificate2? TransportClientCertificate { get; }
}

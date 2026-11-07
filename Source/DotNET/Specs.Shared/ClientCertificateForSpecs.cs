// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Http;

/// <summary>A test-only proxy base for internal transport seams.</summary>
public class ClientCertificateForSpecs : CredentialHeaderPreservationForSpecs, ICanReadClientCertificate
{
    /// <inheritdoc/>
    public X509Certificate2? TransportClientCertificate => Certificate();

    /// <summary>Supplies the certificate configured by a spec.</summary>
    /// <returns>The transport certificate, if any.</returns>
    public virtual X509Certificate2? Certificate() => null;
}

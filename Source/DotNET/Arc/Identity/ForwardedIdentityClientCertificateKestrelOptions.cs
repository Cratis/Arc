// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Requests optional transport certificates while admission remains a per-reader decision.
/// </summary>
/// <param name="arcOptions">The Arc options.</param>
internal sealed class ForwardedIdentityClientCertificateKestrelOptions(IOptions<ArcOptions> arcOptions) : IConfigureOptions<KestrelServerOptions>
{
    /// <inheritdoc/>
    public void Configure(KestrelServerOptions options)
    {
        if (!arcOptions.Value.ForwardedIdentity.ClientCertificate.Required) return;
        options.ConfigureHttpsDefaults(https =>
        {
            if (https.ClientCertificateMode != ClientCertificateMode.NoCertificate) return;
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.AllowAnyClientCertificate();
        });
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop.given;

public class host_https_defaults : Specification
{
    protected readonly Func<X509Certificate2, X509Chain?, SslPolicyErrors, bool> Validation = (_, _, _) => false;
    protected readonly Func<Microsoft.AspNetCore.Connections.ConnectionContext?, string?, X509Certificate2?> Selection = (_, _) => null;
    protected HttpsConnectionAdapterOptions Https;

    protected void Configure(bool arcFirst)
    {
        var builder = WebApplication.CreateBuilder();
        if (arcFirst) AddArc(builder);
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificateSelector = Selection;
            https.SslProtocols = SslProtocols.None;
            https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            https.ClientCertificateValidation = Validation;
        }));
        if (!arcFirst) AddArc(builder);
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(https => Https = https)));
        using var app = builder.Build();
        _ = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
    }

    static void AddArc(WebApplicationBuilder builder) => builder.AddCratisArc(options => options.ForwardedIdentity.ClientCertificate.Required = true);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder.given;

public class an_installation : Specification
{
    protected ServiceCollection _services;
    protected VerifiedCallersBuilder _builder;
    protected string _publicKey;
    protected Exception _exception;

    void Establish()
    {
        _services = new();
        _builder = new();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _publicKey = key.ExportSubjectPublicKeyInfoPem();
    }

    protected void Configure() => _builder.ForResource("https://resource.example", "audience", "test")
        .UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer("https://issuer.example")
            .AddPublicKey("test", _publicKey).UseProcessLocalNonceStore());

    public class PersonDirectory : ICallerPersonDirectory
    {
        public Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken) => Task.FromResult(CallerPersonFound.NotFound());
    }
}

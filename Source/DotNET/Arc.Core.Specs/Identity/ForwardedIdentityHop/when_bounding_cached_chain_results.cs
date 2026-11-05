// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Arc.Identity.for_ForwardedIdentityHop.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_bounding_cached_chain_results : Specification
{
    int _bounded;
    int _invalidated;

    void Because()
    {
        using var authority = new certificates();
        using var replacement = new certificates();
        var options = new ForwardedIdentityClientCertificateOptions
        {
            Required = true,
            TrustedCertificateAuthoritiesPath = authority.Bundle,
            AllowedDnsNames = ["authproxy.internal"]
        };
        var trust = ForwardedIdentityClientCertificateTrust.For(authority.Bundle, TimeProvider.System);
        trust.Load(options.ReloadInterval, NullLogger.Instance);
        var raw = authority.Leaf().RawData;
        var original = raw[^1];
        for (var index = 1; index <= 65; index++)
        {
            // Alter only the signature bytes: retain parseable certificates with distinct fingerprints.
            raw[^1] = (byte)(original ^ index);
            using var invalid = X509Certificate2.CreateFromPem(PemEncoding.WriteString("CERTIFICATE", raw));
            trust.Admit(invalid, options, NullLogger.Instance);
        }
        _bounded = trust.CachedChainCount;
        File.WriteAllText(authority.Bundle, replacement.Root.ExportCertificatePem());
        trust.Load(options.ReloadInterval, NullLogger.Instance);
        _invalidated = trust.CachedChainCount;
    }

    [Fact] void should_bound_the_current_generation_to_sixty_four_entries() => _bounded.ShouldEqual(64);
    [Fact] void should_invalidate_chain_results_for_a_new_bundle_generation() => _invalidated.ShouldEqual(0);
}

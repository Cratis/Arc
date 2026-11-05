// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.for_ForwardedIdentityHop.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_validating_client_certificates : Specification
{
    readonly Dictionary<string, bool> _results = [];

    void Because()
    {
        using var trusted = new certificates();
        using var other = new certificates();
        var options = new ArcOptions();
        options.ForwardedIdentity.ClientCertificate = new()
        {
            Required = true,
            TrustedCertificateAuthoritiesPath = trusted.Bundle,
            AllowedDnsNames = ["authproxy.internal"],
            ReloadInterval = TimeSpan.FromSeconds(5)
        };
        var monitor = Substitute.For<IOptionsMonitor<ArcOptions>>();
        monitor.CurrentValue.Returns(options);
        var clock = new Clock();
        using var store = new ForwardedIdentityClientCertificateTrustStore(monitor, NullLogger<ForwardedIdentityClientCertificateTrustStore>.Instance, clock);
        store.ValidateConfiguration();
        _results["valid"] = store.IsTrusted(trusted.Leaf());
        _results["wrong-ca"] = store.IsTrusted(other.Leaf());
        _results["expired"] = store.IsTrusted(trusted.Leaf(expired: true));
        _results["wrong-san"] = store.IsTrusted(trusted.Leaf("other.internal"));
        _results["cn-only"] = store.IsTrusted(trusted.Leaf(null));
        _results["wildcard"] = store.IsTrusted(trusted.Leaf("*.internal"));
        _results["case"] = store.IsTrusted(trusted.Leaf("AUTHPROXY.internal"));
        _results["wrong-eku"] = store.IsTrusted(trusted.Leaf(clientAuth: false));
        File.WriteAllText(trusted.Bundle, trusted.Root.ExportCertificatePem() + "\n" + other.Root.ExportCertificatePem());
        clock.Advance();
        _results["rotation-overlap"] = store.IsTrusted(other.Leaf());
        File.WriteAllText(trusted.Bundle, other.Root.ExportCertificatePem());
        clock.Advance();
        _results["retired-root"] = store.IsTrusted(trusted.Leaf());
        File.WriteAllText(trusted.Bundle, "malformed");
        clock.Advance();
        _results["malformed-reload"] = store.IsTrusted(other.Leaf());
        File.WriteAllText(trusted.Bundle, other.Root.ExportCertificatePem());
        clock.Advance();
        _results["recovered"] = store.IsTrusted(other.Leaf());
        File.Delete(trusted.Bundle);
        clock.Advance();
        _results["missing-reload"] = store.IsTrusted(other.Leaf());
    }

    [Fact] void should_accept_the_exact_admitted_peer() => _results["valid"].ShouldBeTrue();
    [Fact] void should_allow_both_authorities_during_rotation() => _results["rotation-overlap"].ShouldBeTrue();
    [Fact] void should_recover_after_a_valid_replacement() => _results["recovered"].ShouldBeTrue();
    [Fact] void should_retain_the_last_good_bundle_on_bad_reload() => _results["malformed-reload"].ShouldBeTrue();
    [Fact] void should_retain_the_last_good_bundle_when_missing() => _results["missing-reload"].ShouldBeTrue();
    [Fact] void should_match_dns_names_case_insensitively() => _results["case"].ShouldBeTrue();
    [Fact] void should_refuse_every_negative_control() => new[] { "wrong-ca", "expired", "wrong-san", "cn-only", "wildcard", "wrong-eku", "retired-root" }.All(name => !_results[name]).ShouldBeTrue();

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance() => _now += TimeSpan.FromSeconds(5);
    }
}

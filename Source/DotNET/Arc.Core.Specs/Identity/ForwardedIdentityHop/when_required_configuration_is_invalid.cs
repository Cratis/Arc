// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityHop.given;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.ForwardedIdentityHop;

public class when_required_configuration_is_invalid : Specification
{
    readonly List<Exception?> _failures = [];

    void Because()
    {
        using var authority = new certificates();
        var candidates = new ForwardedIdentityClientCertificateOptions[]
        {
            new() { Required = true, AllowedDnsNames = ["authproxy.internal"] },
            new() { Required = true, TrustedCertificateAuthoritiesPath = authority.Bundle },
            new() { Required = true, TrustedCertificateAuthoritiesPath = authority.Bundle, AllowedDnsNames = ["authproxy.internal"], ReloadInterval = TimeSpan.Zero },
            new() { Required = true, TrustedCertificateAuthoritiesPath = authority.Bundle + ".missing", AllowedDnsNames = ["authproxy.internal"] }
        };
        foreach (var candidate in candidates)
        {
            var monitor = Substitute.For<IOptionsMonitor<ArcOptions>>();
            var options = new ArcOptions();
            options.ForwardedIdentity.ClientCertificate = candidate;
            monitor.CurrentValue.Returns(options);
            using var store = new ForwardedIdentityClientCertificateTrustStore(monitor, NullLogger<ForwardedIdentityClientCertificateTrustStore>.Instance);
            _failures.Add(Catch.Exception(store.ValidateConfiguration));
        }
    }

    [Fact] void should_refuse_every_invalid_required_configuration() => _failures.All(failure => failure is OptionsValidationException).ShouldBeTrue();
}

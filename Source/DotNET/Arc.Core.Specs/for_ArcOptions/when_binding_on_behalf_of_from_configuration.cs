// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.for_ArcOptions;

public class when_binding_on_behalf_of_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:OnBehalfOf:Enabled"] = "true",
            ["Cratis:Arc:OnBehalfOf:Issuer"] = "https://authproxy.example.com",
            ["Cratis:Arc:OnBehalfOf:Audience"] = "https://app.example.com",
            ["Cratis:Arc:OnBehalfOf:ClockSkew"] = "00:00:10",
            ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "authproxy-2026",
            ["Cratis:Arc:OnBehalfOf:SigningKeys:0:PublicKeyPem"] = "-----BEGIN PUBLIC KEY-----"
        })
        .Build().GetSection("Cratis:Arc").Bind(_options);

    [Fact] void should_enable_on_behalf_of() => _options.OnBehalfOf.Enabled.ShouldBeTrue();
    [Fact] void should_bind_the_issuer() => _options.OnBehalfOf.Issuer.ShouldEqual("https://authproxy.example.com");
    [Fact] void should_bind_the_audience() => _options.OnBehalfOf.Audience.ShouldEqual("https://app.example.com");
    [Fact] void should_bind_the_clock_skew() => _options.OnBehalfOf.ClockSkew.ShouldEqual(TimeSpan.FromSeconds(10));
    [Fact] void should_bind_the_key_identifier() => _options.OnBehalfOf.SigningKeys.Single().KeyId.ShouldEqual("authproxy-2026");
    [Fact] void should_bind_the_public_key() => _options.OnBehalfOf.SigningKeys.Single().PublicKeyPem.ShouldEqual("-----BEGIN PUBLIC KEY-----");
}

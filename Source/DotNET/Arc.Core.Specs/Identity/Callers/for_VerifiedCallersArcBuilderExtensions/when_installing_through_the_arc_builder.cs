// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersArcBuilderExtensions;

public class when_installing_through_the_arc_builder : for_VerifiedCallersBuilder.given.an_installation
{
    IArcBuilder _arc;
    IArcBuilder _result;
    ServiceProvider _provider;

    void Establish()
    {
        _services.AddVerifiedCallers();
        _arc = Substitute.For<IArcBuilder>();
        _arc.Services.Returns(_services);
    }

    void Because()
    {
        _result = _arc.WithVerifiedCallers(callers => callers.ForResource("https://resource.example", "audience", "test")
            .UsePersonDirectory<PersonDirectory>()
            .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer("https://issuer.example").AddPublicKey("test", _publicKey).UseProcessLocalNonceStore()));
        _provider = _services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_return_the_arc_builder() => _result.ShouldEqual(_arc);
    [Fact] void should_install_exactly_one_source() => _services.Count(descriptor => descriptor.ServiceType == typeof(IVerifiedCredentialSource)).ShouldEqual(1);
    [Fact] void should_replace_the_inert_installation() => _services.Count(descriptor => descriptor.ServiceType == typeof(VerifiedCallerInstallation)).ShouldEqual(1);
    [Fact] void should_replace_the_inert_catalog() => _services.Count(descriptor => descriptor.ServiceType == typeof(CallerResourceCatalog)).ShouldEqual(1);
    [Fact] void should_bind_the_resource() => _provider.GetRequiredService<VerifiedCallerInstallation>().Resource.ShouldEqual("https://resource.example");
    [Fact] void should_bind_the_environment() => _provider.GetRequiredService<VerifiedCallerInstallation>().Environment.ShouldEqual("test");
    [Fact] void should_bind_the_audience()
    {
        _provider.GetRequiredService<CallerResourceCatalog>().TryGetAudience("https://resource.example", out var audience);
        audience.ShouldEqual("audience");
    }
}

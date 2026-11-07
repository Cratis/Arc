// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens;

[PerFactSpecificationLifecycle]
public class when_installing : given.a_pat_module
{
    [Fact]
    public async Task should_install_without_forwarding_keys_and_fail_closed_without_authorities()
    {
        var services = new ServiceCollection();
        var arc = Substitute.For<IArcBuilder>();
        arc.Services.Returns(services);
        arc.WithPersonalAccessTokens(new("direct") { AllowedScopes = ["read"], ReservedLegacyPrefixes = ["dpat_"] },
            callers => callers.ForResource("resource-a", "https://resource-a.example", "test").UsePersonDirectory<PersonDirectory>());
        services.AddSingleton(_context);
        services.AddSingleton(_clock);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IPersonalAccessTokens>();
        (await service.Issue(TimeSpan.FromDays(1), ["read"], TestContext.Current.CancellationToken)).Token.ShouldBeNull();
        var source = scope.ServiceProvider.GetServices<IVerifiedCredentialSource>().Single();
        source.ShouldBeOfExactType<PatCredentialSource>();
        var raw = PatFormat.Create(new("direct")).Reveal();
        (await source.Verify(Presentation(raw), TestContext.Current.CancellationToken)).Refusal.ShouldNotBeNull();
        services.Any(descriptor => descriptor.ServiceType.Name.Contains("ForwardedIdentityV2", StringComparison.Ordinal)).ShouldBeFalse();
    }

    [Fact]
    public void should_preserve_the_existing_signed_forwarding_requirements()
    {
        var services = new ServiceCollection();
        var existing = new VerifiedCallersBuilder().ForResource("resource-a", "https://resource-a.example", "test").UsePersonDirectory<PersonDirectory>();
        Catch.Exception(() => existing.Install(services)).ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
        services.Count.ShouldEqual(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7776001)]
    public void should_refuse_invalid_installation_lifetime_caps(int seconds)
    {
        Catch.Exception(() => PatSettings.Create(new("direct") { MaximumLifetime = TimeSpan.FromSeconds(seconds) }, _settings.Target)).ShouldBeOfExactType<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("direct_pat_")]
    [InlineData("direct_")]
    [InlineData("not-a-namespace")]
    [InlineData("nönascii_")]
    public void should_refuse_invalid_or_overlapping_reserved_namespaces(string prefix)
    {
        Catch.Exception(() => PatSettings.Create(new("direct") { ReservedLegacyPrefixes = [prefix] }, _settings.Target)).ShouldBeOfExactType<ArgumentException>();
    }
}

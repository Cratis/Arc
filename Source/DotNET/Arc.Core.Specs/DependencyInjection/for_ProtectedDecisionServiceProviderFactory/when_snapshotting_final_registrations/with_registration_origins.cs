// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_snapshotting_final_registrations;

public class with_registration_origins : a_certified_validator
{
    [Fact]
    public void should_not_copy_origin_when_a_second_adapter_builds_cloned_descriptors()
    {
        Build();
        IServiceCollection cloneServices = new ServiceCollection();
        foreach (var descriptor in _services) cloneServices.Add(descriptor);
        using var clone = (ServiceProvider)cloneServices.BuildProtectedDecisionServiceProvider();
        using var scope = clone.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        attestation.Root.Manifest.HasTrustedReader().ShouldBeFalse();
        attestation.Root.Manifest.Discovered(typeof(ReadingValidator)).ShouldBeFalse();
        attestation.Root.Manifest.BindingsFor(typeof(ReadingValidator)).Select(attestation.Root.Manifest.OriginOf).ShouldEachConformTo(origin => origin is null);
    }

    [Fact]
    public void should_freeze_two_independent_snapshots_from_the_same_original_collection()
    {
        Build();
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out var first).ShouldBeTrue();
        _services.AddSingleton(new Opaque());
        using var later = (ServiceProvider)_services.BuildProtectedDecisionServiceProvider();
        using var scope = later.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var second).ShouldBeTrue();
        first.Root.Manifest.BindingsFor(typeof(Opaque)).ShouldBeEmpty();
        second.Root.Manifest.BindingsFor(typeof(Opaque)).Length.ShouldEqual(1);
        first.Root.Manifest.ShouldNotBeSame(second.Root.Manifest);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("keyed")]
    [InlineData("duplicate")]
    public void should_retain_registration_defects_instead_of_erasing_them(string binding)
    {
        if (binding == "application") _services.AddTransient<ReadingValidator>();
        if (binding == "keyed") _services.AddKeyedTransient<ReadingValidator>("key");
        if (binding == "duplicate") _services.Add(_services.Single(descriptor => descriptor.ServiceType == typeof(ReadingValidator)));
        Build();
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out var scope).ShouldBeTrue();
        scope.Root.Manifest.BindingsFor(typeof(ReadingValidator)).Length.ShouldEqual(2);
    }
}

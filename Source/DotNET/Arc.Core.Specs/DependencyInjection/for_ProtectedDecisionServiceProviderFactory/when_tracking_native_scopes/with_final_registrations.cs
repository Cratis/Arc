// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_final_registrations : Specification
{
    bool _sameBuilder;
    ServiceDescriptor[] _applicationDescriptors;
    IReadOnlyList<ServiceDescriptor> _snapshot;
    string _value;

    void Because()
    {
        var factory = new ProtectedDecisionServiceProviderFactory();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton("early");
        var builder = factory.CreateBuilder(services);
        _sameBuilder = ReferenceEquals(services, builder);
        services.AddSingleton("late");
        _applicationDescriptors = services.ToArray();
        using var root = (ServiceProvider)factory.CreateServiceProvider(builder);
        services.Clear();
        services.AddSingleton("after build");
        using var scope = root.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation);
        _snapshot = attestation!.Root.Registrations;
        _value = scope.ServiceProvider.GetRequiredService<string>();
    }

    [Fact] void should_preserve_builder_identity() => _sameBuilder.ShouldBeTrue();
    [Fact] void should_build_the_final_selection() => _value.ShouldEqual("late");
    [Fact] void should_retain_duplicate_descriptors_in_order() => _snapshot.Take(2).ShouldEqual(_applicationDescriptors);
    [Fact] void should_snapshot_the_internal_probes_too() => _snapshot.Count.ShouldEqual(4);
}

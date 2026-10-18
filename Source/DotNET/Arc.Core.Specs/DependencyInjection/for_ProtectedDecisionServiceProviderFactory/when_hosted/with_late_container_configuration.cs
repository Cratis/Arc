// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_hosted;

public class with_late_container_configuration : Specification
{
    IHost _host;
    ProtectedDecisionRootAttestation _root;
    object _late;

    void Establish()
    {
        _late = new();
        _host = new HostBuilder()
            .UseServiceProviderFactory(new ProtectedDecisionServiceProviderFactory())
            .ConfigureContainer<IServiceCollection>(services => services.AddSingleton(_late))
            .Build();
    }

    void Because()
    {
        using var scope = _host.Services.CreateScope();
        ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation).ShouldBeTrue();
        _root = attestation.Root;
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_build_the_actual_host_root_with_the_factory() => _root.Provider.ShouldBeSame(_host.Services);
    [Fact] void should_capture_late_configure_container_registrations() => _root.Manifest.BindingsFor(typeof(object)).Single().ImplementationInstance.ShouldBeSame(_late);
    [Fact] void should_not_claim_chronicle_read_support_from_a_host_factory_alone() => _root.Manifest.HasTrustedReader().ShouldBeFalse();
}

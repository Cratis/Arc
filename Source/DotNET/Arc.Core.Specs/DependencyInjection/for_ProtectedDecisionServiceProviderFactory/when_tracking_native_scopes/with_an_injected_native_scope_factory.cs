// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_an_injected_native_scope_factory : Specification
{
    bool _accepted;
    bool _correctRoot;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ScopeOwner>();
        var factory = new ProtectedDecisionServiceProviderFactory();
        using var root = (ServiceProvider)factory.CreateServiceProvider(services);
        using var scope = root.GetRequiredService<ScopeOwner>().CreateScope();
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation);
        _correctRoot = ReferenceEquals(root, attestation?.Root.Provider);
    }

    [Fact] void should_attest_a_scope_not_created_through_a_root_wrapper() => _accepted.ShouldBeTrue();
    [Fact] void should_bind_the_host_root() => _correctRoot.ShouldBeTrue();

    sealed class ScopeOwner(IServiceScopeFactory factory)
    {
        public IServiceScope CreateScope() => factory.CreateScope();
    }
}

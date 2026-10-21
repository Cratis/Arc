// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_copied_probe_descriptors : given.two_roots
{
    bool _accepted;
    bool _probeResolved;

    void Because()
    {
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out var original);
        IServiceCollection copied = new ServiceCollection();
        foreach (var descriptor in original!.Root.Registrations)
        {
            copied.Add(descriptor);
        }
        using var clone = copied.BuildServiceProvider();
        using var scope = clone.CreateScope();
        _probeResolved = scope.ServiceProvider.GetService<ProtectedDecisionScopeProbe>() is not null;
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out _);
    }

    [Fact] void should_resolve_the_original_probe_callback() => _probeResolved.ShouldBeTrue();
    [Fact] void should_not_attest_the_clone() => _accepted.ShouldBeFalse();
}

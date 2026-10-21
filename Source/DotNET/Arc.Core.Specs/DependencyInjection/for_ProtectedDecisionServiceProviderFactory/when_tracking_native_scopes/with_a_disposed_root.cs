// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_a_disposed_root : given.two_roots
{
    bool _warmAccepted;
    bool _coldAccepted;
    bool _rootAccepted;
    bool _canonicalAccepted;

    void Because()
    {
        var canonical = _first.GetRequiredService<IServiceProvider>();
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _);
        using var cold = _first.CreateScope();
        _first.Dispose();
        _warmAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _);
        _coldAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(cold.ServiceProvider, out _);
        _rootAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_first, out _);
        _canonicalAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(canonical, out _);
    }

    [Fact] void should_invalidate_an_attested_scope() => _warmAccepted.ShouldBeFalse();
    [Fact] void should_refuse_an_unattested_child() => _coldAccepted.ShouldBeFalse();
    [Fact] void should_refuse_the_disposed_root() => _rootAccepted.ShouldBeFalse();
    [Fact] void should_refuse_the_disposed_canonical_alias() => _canonicalAccepted.ShouldBeFalse();
}

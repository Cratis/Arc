// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_disposed_scopes : given.two_roots
{
    bool _warmAccepted;
    bool _coldAccepted;
    bool _initiallyAccepted;

    void Because()
    {
        _initiallyAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _);
        _scope.Dispose();
        _warmAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _);
        using var cold = _first.CreateScope();
        cold.Dispose();
        _coldAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(cold.ServiceProvider, out _);
    }

    [Fact] void should_initially_accept_the_live_scope() => _initiallyAccepted.ShouldBeTrue();
    [Fact] void should_refuse_a_previously_attested_disposed_scope() => _warmAccepted.ShouldBeFalse();
    [Fact] void should_refuse_a_disposed_scope_without_a_probe() => _coldAccepted.ShouldBeFalse();
}

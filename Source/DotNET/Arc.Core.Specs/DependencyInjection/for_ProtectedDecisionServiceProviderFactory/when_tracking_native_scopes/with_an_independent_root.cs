// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_an_independent_root : given.two_roots
{
    bool _accepted;
    ProtectedDecisionScopeAttestation _original;
    ProtectedDecisionScopeAttestation _independent;

    void Because()
    {
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _original!);
        using var other = _second.CreateScope();
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(other.ServiceProvider, out _independent!);
    }

    [Fact] void should_accept_under_its_own_attestation() => _accepted.ShouldBeTrue();
    [Fact] void should_not_borrow_the_first_roots_attestation() => _independent.Root.ShouldNotBeSame(_original.Root);
    [Fact] void should_bind_the_second_root() => _independent.Root.Provider.ShouldBeSame(_second);
}

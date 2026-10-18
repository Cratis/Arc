// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_a_native_scope : given.two_roots
{
    bool _accepted;
    ProtectedDecisionScopeAttestation _attestation;

    void Because() => _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out _attestation!);

    [Fact] void should_accept_the_scope() => _accepted.ShouldBeTrue();
    [Fact] void should_bind_the_exact_provider() => _attestation.Provider.ShouldBeSame(_scope.ServiceProvider);
    [Fact] void should_bind_the_correct_root() => _attestation.Root.Provider.ShouldBeSame(_first);
    [Fact] void should_use_a_sealed_native_provider_type() => _attestation.Root.ScopeProviderType.IsSealed.ShouldBeTrue();
}

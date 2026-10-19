// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_a_nested_scope : given.two_roots
{
    bool _accepted;
    bool _sameRoot;
    bool _differentScope;

    void Because()
    {
        ProtectedDecisionProviderAttestations.TryGetAttestation(_scope.ServiceProvider, out var parent);
        using var nested = _scope.ServiceProvider.CreateScope();
        _accepted = ProtectedDecisionProviderAttestations.TryGetAttestation(nested.ServiceProvider, out var child);
        _sameRoot = ReferenceEquals(parent!.Root, child!.Root);
        _differentScope = !ReferenceEquals(parent.Provider, child.Provider);
    }

    [Fact] void should_accept_the_nested_scope() => _accepted.ShouldBeTrue();
    [Fact] void should_preserve_root_identity() => _sameRoot.ShouldBeTrue();
    [Fact] void should_keep_scope_identity_distinct() => _differentScope.ShouldBeTrue();
}

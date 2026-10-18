// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_concurrent_scopes_from_two_roots : given.two_roots
{
    bool[] _results;

    async Task Because()
    {
        using var start = new Barrier(2);
        var first = Task.Run(() => AttestScopes(_first, start));
        var second = Task.Run(() => AttestScopes(_second, start));
        _results = (await Task.WhenAll(first, second)).SelectMany(results => results).ToArray();
    }

    [Fact] void should_attest_every_scope_to_its_own_root() => _results.ShouldEachConformTo(result => result);
    [Fact] void should_check_both_roots() => _results.Length.ShouldEqual(128);

    static bool[] AttestScopes(ServiceProvider root, Barrier start)
    {
        start.SignalAndWait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
        return Enumerable.Range(0, 64).Select(_ =>
        {
            using var scope = root.CreateScope();
            return ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out var attestation) &&
                ReferenceEquals(attestation.Root.Provider, root) && ReferenceEquals(attestation.Provider, scope.ServiceProvider);
        }).ToArray();
    }
}

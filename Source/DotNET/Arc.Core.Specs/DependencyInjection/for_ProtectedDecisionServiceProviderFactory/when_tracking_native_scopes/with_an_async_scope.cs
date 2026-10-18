// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes;

public class with_an_async_scope : given.two_roots
{
    bool _liveAccepted;
    bool _disposedAccepted;

    async Task Because()
    {
        var scope = _first.CreateAsyncScope();
        _liveAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out _);
        await scope.DisposeAsync();
        _disposedAccepted = ProtectedDecisionProviderAttestations.TryGetAttestation(scope.ServiceProvider, out _);
    }

    [Fact] void should_accept_the_live_scope() => _liveAccepted.ShouldBeTrue();
    [Fact] void should_refuse_the_asynchronously_disposed_scope() => _disposedAccepted.ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Delegation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_delegation_uses_the_consumers_scope : given.a_delegated_consumer
{
    IServiceProvider _authorityScope;
    bool _disposedBeforeScope;

    void Establish() => _services.AddScoped(services =>
    {
        _authorityScope = services;
        return _authority;
    });

    async Task Because()
    {
        await InvokeConsumer();
        _disposedBeforeScope = ((IDisposable)_authority).ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDisposable.Dispose));
        DisposeScope();
    }

    [Fact] void should_use_the_consumers_existing_scope() => ReferenceEquals(_authorityScope, CurrentScope).ShouldBeTrue();
    [Fact] void should_not_dispose_the_authority_before_the_scope_ends() => _disposedBeforeScope.ShouldBeFalse();
    [Fact] void should_dispose_the_authority_with_the_consumers_scope() => ((IDisposable)_authority).Received(1).Dispose();
}

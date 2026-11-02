// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope;

public class when_disposing_and_transferring_concurrently : Specification
{
    const int Iterations = 1000;
    List<IServiceScope> _scopes;

    void Establish() => _scopes = [];

    void Because()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var scope = Substitute.For<IServiceScope>();
            _scopes.Add(scope);
            var holder = new ObservableQuerySubscriptionServiceScope(scope);
            var lifetime = new StreamingQuerySubscription(CancellationToken.None);
            using var barrier = new Barrier(2);
            var deadline = TimeSpan.FromSeconds(10);
            var dispose = Task.Run(() =>
            {
                barrier.SignalAndWait(deadline);
                holder.Dispose();
            });
            var transfer = Task.Run(() =>
            {
                barrier.SignalAndWait(deadline);
                holder.TryTransferTo(lifetime);
            });
            Task.WaitAll([dispose, transfer], deadline);
            lifetime.Dispose();
        }
    }

    [Fact] void should_dispose_every_scope_exactly_once() => _scopes.TrueForAll(_ => _.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IDisposable.Dispose)) == 1).ShouldBeTrue();
}

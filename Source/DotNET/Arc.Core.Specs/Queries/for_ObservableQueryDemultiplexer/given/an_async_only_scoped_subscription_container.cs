// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class an_async_only_scoped_subscription_container : a_scoped_subscription_container
{
    protected ConcurrentQueue<AsyncOnlyProbe> _asyncProbes;
    protected ConcurrentQueue<AsyncOnlyProbe> _asyncResolvedDuringPerform;

    void Establish()
    {
        _asyncProbes = [];
        _asyncResolvedDuringPerform = [];
        var performResult = _performResult;
        _performResult = token =>
        {
            _asyncResolvedDuringPerform.Enqueue(_httpRequestContextAccessor.Current.RequestServices.GetRequiredService<AsyncOnlyProbe>());
            return performResult(token);
        };
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        base.ConfigureGuards(services, guardTypes);
        services.AddScoped(_ =>
        {
            var probe = new AsyncOnlyProbe(_signals);
            _asyncProbes?.Enqueue(probe);
            return probe;
        });
    }

    public sealed class AsyncOnlyProbe(condition_pulse signals) : IAsyncDisposable
    {
        int _disposals;

        public int Disposals => Volatile.Read(ref _disposals);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            signals.Signal();
            return ValueTask.CompletedTask;
        }
    }
}

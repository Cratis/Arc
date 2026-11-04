// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class an_async_only_scoped_websocket_container : a_scoped_websocket_container
{
    protected ConcurrentQueue<an_async_only_scoped_subscription_container.AsyncOnlyProbe> _asyncProbes;
    protected ConcurrentQueue<an_async_only_scoped_subscription_container.AsyncOnlyProbe> _asyncResolvedDuringPerform;

    void Establish()
    {
        _asyncProbes = [];
        _asyncResolvedDuringPerform = [];
        var performResult = _performResult;
        _performResult = token =>
        {
            _asyncResolvedDuringPerform.Enqueue(_httpRequestContextAccessor.Current.RequestServices.GetRequiredService<an_async_only_scoped_subscription_container.AsyncOnlyProbe>());
            return performResult(token);
        };
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        base.ConfigureGuards(services, guardTypes);
        services.AddScoped(_ =>
        {
            var probe = new an_async_only_scoped_subscription_container.AsyncOnlyProbe(_signals);
            _asyncProbes?.Enqueue(probe);
            return probe;
        });
    }
}

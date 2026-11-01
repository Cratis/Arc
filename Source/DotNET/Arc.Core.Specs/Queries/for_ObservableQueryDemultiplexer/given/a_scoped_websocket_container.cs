// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// A WebSocket connection over a real container that registers a scoped, disposable probe, so a spec can observe which
/// service scope a subscription context resolves through and when that scope is disposed.
/// </summary>
public class a_scoped_websocket_container : a_guarded_websocket_connection
{
    protected ConcurrentQueue<a_scoped_subscription_container.ScopedProbe> _probes;
    protected ConcurrentQueue<a_scoped_subscription_container.ScopedProbe> _resolvedDuringPerform;
    protected ConcurrentQueue<IHttpRequestContext> _performContexts;
    protected a_scoped_subscription_container.ScopedProbe _rootProbe;

    /// <summary>
    /// What the stubbed pipeline does once it has resolved the probe. Defaults to a streaming result over the subject.
    /// </summary>
    protected Func<QueryResult> _result;

    /// <summary>
    /// How the stubbed pipeline produces its result once the probe is resolved. Defaults to <see cref="_result"/>;
    /// a spec replaces it to block the query or to resolve through the context again.
    /// </summary>
    protected Func<CancellationToken, Task<QueryResult>> _performResult;

    void Establish()
    {
        _probes = [];
        _resolvedDuringPerform = [];
        _performContexts = [];
        _httpRequestContextAccessor = new HttpRequestContextAccessor();
        _result = () =>
        {
            var result = QueryResult.Success(CorrelationId.New());
            result.Data = _subject;
            return result;
        };

        _performResult = _ => Task.FromResult(_result());
        UseGuards(_configuredGuards);
        _rootProbe = _guardedServiceProvider.GetRequiredService<a_scoped_subscription_container.ScopedProbe>();

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var context = _httpRequestContextAccessor.Current;
                _performContexts.Enqueue(context);
                var first = context.RequestServices.GetRequiredService<a_scoped_subscription_container.ScopedProbe>();
                var second = context.RequestServices.GetRequiredService<a_scoped_subscription_container.ScopedProbe>();
                first.ShouldBeSame(second);
                first.Tenant = context.Headers.TryGetValue("x-tenant", out var tenant) ? tenant : null;
                _resolvedDuringPerform.Enqueue(first);
                return _performResult(call.Arg<CancellationToken>());
            });
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes) =>
        services.AddScoped(_ =>
        {
            var probe = new a_scoped_subscription_container.ScopedProbe(_signals);
            _probes?.Enqueue(probe);
            return probe;
        });
}

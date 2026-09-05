// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservableSSE.when_handling_connection.given;

public class a_guarded_client_enumerable_observable_sse : Specification
{
    protected const string QueryName = "MyApp.Queries.GuardedQuery";

    protected IHttpRequestContext _context;
    protected IObservableQueryEmissionGuards _emissionGuards;
    protected ConcurrentQueue<ObservableQueryEmissionContext> _guardCalls;
    protected ConcurrentQueue<string> _messages;
    protected ClaimsPrincipal _principal;
    protected QueryContext _queryContext;
    protected IOptions<ArcOptions> _arcOptions;
    protected ClientEnumerableObservableSSE<string> _observable;

    /// <summary>
    /// The verdict for an emission. Assign in a spec's own Establish — it is read when the emission happens.
    /// </summary>
    protected Func<ObservableQueryEmissionContext, ObservableQueryEmissionVerdict> _verdict = _ => ObservableQueryEmissionVerdict.Allow;

    void Establish()
    {
        _guardCalls = [];
        _messages = [];
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "the-caller")], "test"));
        _queryContext = new QueryContext(QueryName, CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["id"] = 42 });
        _arcOptions = Options.Create(new ArcOptions());

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _context.User.Returns(_principal);
        _context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        var readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));

        _emissionGuards = Substitute.For<IObservableQueryEmissionGuards>();
        _emissionGuards.HasGuards.Returns(true);
        _emissionGuards.Guard(Arg.Any<ObservableQueryEmissionContext>())
            .Returns(callInfo =>
            {
                var emissionContext = callInfo.Arg<ObservableQueryEmissionContext>();
                _guardCalls.Enqueue(emissionContext);
                return Task.FromResult(_verdict(emissionContext));
            });

        _observable = new ClientEnumerableObservableSSE<string>(
            _queryContext,
            TwoItems(),
            readModelInterceptors,
            _arcOptions,
            _emissionGuards,
            Substitute.For<ILogger<IClientObservable>>());
    }

    protected IEnumerable<QueryResult> WrittenResults =>
        _messages
            .Where(_ => _.StartsWith("data: ", StringComparison.Ordinal))
            .Select(_ => JsonSerializer.Deserialize<QueryResult>(_["data: ".Length..].Trim(), _arcOptions.Value.JsonSerializerOptions))
            .Where(_ => _ is not null)
            .Select(_ => _!);

    protected async Task RunConnection() =>
        await _observable.HandleConnection(_context).WaitAsync(TimeSpan.FromSeconds(5));

    static async IAsyncEnumerable<string> TwoItems([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in new[] { "event-store-a", "event-store-b" })
        {
            await Task.Delay(10, cancellationToken);
            yield return item;
        }
    }
}

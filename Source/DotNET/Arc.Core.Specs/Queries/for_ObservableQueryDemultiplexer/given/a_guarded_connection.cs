// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// A demultiplexer wired to a real <see cref="ObservableQueryEmissionGuards"/> over a single application guard, so the
/// specs exercise discovery, per-subscription resolution and verdict aggregation the way an application would — not a
/// stand-in for them.
/// </summary>
public class a_guarded_connection : an_observable_query_demultiplexer
{
    protected const string QueryName = "MyApp.Queries.GuardedQuery";
    protected const string FirstQueryId = "query-1";
    protected const string SecondQueryId = "query-2";

    /// <summary>
    /// What the client actually puts on the wire — strings, because a query string cannot carry anything else.
    /// </summary>
    protected static readonly Dictionary<string, string?> RawArguments = new() { ["id"] = "42" };

    /// <summary>
    /// What the query pipeline publishes on the query context after coercing <see cref="RawArguments"/> to the
    /// declared parameter types. A guard has to be told these, not the strings that came in over the wire.
    /// </summary>
    protected static readonly QueryArguments CoercedArguments = new() { ["id"] = 42 };

    protected ConcurrentQueue<ObservableQueryEmissionContext> _guardCalls;
    protected ConcurrentQueue<CorrelationId> _correlationIds;
    protected Subject<IEnumerable<string>> _subject;
    protected IQueryHealthTracker _healthTracker;
    protected ClaimsPrincipal _principal;
    protected IServiceProvider _guardedServiceProvider;
    protected object _streamingData;
    protected string[] _queryIds = [FirstQueryId];

    /// <summary>
    /// The verdict the application guard gives for an emission. Assign in a spec's own Establish — it is read when
    /// the emission happens, which is long after every Establish has run.
    /// </summary>
    protected Func<ObservableQueryEmissionContext, ObservableQueryEmissionVerdict> _verdict = _ => ObservableQueryEmissionVerdict.Allow;

    void Establish()
    {
        _guardCalls = [];
        _correlationIds = [];
        _subject = new Subject<IEnumerable<string>>();
        _streamingData = _subject;
        _healthTracker = Substitute.For<IQueryHealthTracker>();
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "the-caller")], "test"));

        var services = new ServiceCollection();
        services.AddSingleton(new TestEmissionGuard(Evaluate));
        var guardTypes = new List<Type> { typeof(TestEmissionGuard) };
        ConfigureGuards(services, guardTypes);
        _guardedServiceProvider = services.BuildServiceProvider();

        var types = Substitute.For<ITypes>();
        types.FindMultiple<IGuardObservableQueryEmission>().Returns(guardTypes.ToArray());
        var guards = new ObservableQueryEmissionGuards(types, Substitute.For<ILogger<ObservableQueryEmissionGuards>>());

        // Performing a query publishes the coerced arguments on the query context, which is where the demultiplexer
        // reads them from. A bare substitute answers null here, and a null answer sends every spec down the fallback
        // to the raw wire strings — leaving the coerced lookup, and the whole point of it, unexercised.
        _queryContextManager.Current.Returns(new QueryContext(
            QueryName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            CoercedArguments));

        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                // Every subscription gets its own correlation id, which is what a spec uses to tell one
                // subscription's emissions from another's when both run the same query on one connection.
                var correlationId = CorrelationId.New();
                _correlationIds.Enqueue(correlationId);
                var queryResult = QueryResult.Success(correlationId);
                queryResult.Data = _streamingData;
                return Task.FromResult(queryResult);
            });

        // Rebuild the hub over the guarded container and the real guards — the base context wires a substitute that
        // reports no guards, which is the fast path every other spec runs on.
        UseGuards(guards);
    }

    /// <summary>
    /// Rebuilds the hub over a different set of emission guards, so a spec can run the very same connection script
    /// on the no-guard fast path.
    /// </summary>
    /// <param name="guards">The <see cref="IObservableQueryEmissionGuards"/> the hub consults.</param>
    protected void UseGuards(IObservableQueryEmissionGuards guards) =>
        _hub = new ObservableQueryDemultiplexer(
            _queryPipeline,
            _queryContextManager,
            _httpRequestContextAccessor,
            _hostApplicationLifetime,
            _readModelInterceptors,
            _guardedServiceProvider,
            _arcOptions,
            _healthTracker,
            guards,
            _logger);

    /// <summary>
    /// Lets a spec add its own guard — and the services that guard needs — to the application's container.
    /// </summary>
    /// <param name="services">The application's services.</param>
    /// <param name="guardTypes">The guard types discovery yields, in order.</param>
    protected virtual void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
    }

    /// <summary>
    /// A batch every 10 ms until the subscription is torn down — the async-enumerable counterpart of the subject.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the subscription ends.</param>
    /// <returns>The stream of batches.</returns>
    protected static async IAsyncEnumerable<IEnumerable<string>> ABatchEvery10Milliseconds([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < 100; index++)
        {
            await Task.Delay(10, cancellationToken);
            yield return [$"item-{index}"];
        }
    }

    ObservableQueryEmissionVerdict Evaluate(ObservableQueryEmissionContext context)
    {
        _guardCalls.Enqueue(context);
        return _verdict(context);
    }

    /// <summary>
    /// The application's guard, delegating every decision to the spec so a verdict — or a failure — is expressed
    /// where the scenario reads.
    /// </summary>
    /// <param name="evaluate">The decision the spec makes for an emission.</param>
    public class TestEmissionGuard(Func<ObservableQueryEmissionContext, ObservableQueryEmissionVerdict> evaluate) : IGuardObservableQueryEmission
    {
        /// <inheritdoc/>
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context) =>
            Task.FromResult(evaluate(context));
    }
}

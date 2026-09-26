// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class an_observable_query_demultiplexer : Specification
{
    protected IQueryPipeline _queryPipeline;
    protected IQueryContextManager _queryContextManager;
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IHostApplicationLifetime _hostApplicationLifetime;
    protected IReadModelInterceptors _readModelInterceptors;
    protected IServiceProvider _serviceProvider;
    protected IOptions<ArcOptions> _arcOptions;
    protected IObservableQueryEmissionGuards _emissionGuards;
    protected ILogger<ObservableQueryDemultiplexer> _logger;
    protected ObservableQueryDemultiplexer _hub;
    protected readonly condition_pulse _signals = new();

    void Establish()
    {
        _queryPipeline = Substitute.For<IQueryPipeline>();
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        _hostApplicationLifetime.ApplicationStopping.Returns(CancellationToken.None);

        // Pass-through interception by default — each emitted item flows out unchanged. Specs that
        // exercise compliance/PII release override this to assert the streaming path is intercepted.
        _readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));

        // A real container — the hub creates a per-subscription IServiceScope from this, which a bare
        // NSubstitute mock cannot satisfy (it has no working IServiceScopeFactory to resolve).
        _serviceProvider = new ServiceCollection().BuildServiceProvider();

        _arcOptions = Options.Create(new ArcOptions());

        // No guards by default — HasGuards is false on a fresh substitute, which is the opt-in-by-presence fast
        // path every existing spec runs on. Specs that exercise a guard configure this substitute in their own
        // Establish, which runs after this one and is picked up because the hub holds the same instance.
        _emissionGuards = Substitute.For<IObservableQueryEmissionGuards>();

        _logger = Substitute.For<ILogger<ObservableQueryDemultiplexer>>();
        CreateDemultiplexer();
    }

    protected void UseRealQueryContextManager()
    {
        _queryContextManager = new QueryContextManager();
        CreateDemultiplexer();
    }

    void CreateDemultiplexer()
    {
        _hub = new ObservableQueryDemultiplexer(
            _queryPipeline,
            _queryContextManager,
            _httpRequestContextAccessor,
            _hostApplicationLifetime,
            _readModelInterceptors,
            _serviceProvider,
            _arcOptions,
            Substitute.For<IQueryHealthTracker>(),
            _emissionGuards,
            _logger);
    }

    /// <summary>
    /// Waits until the condition holds, and fails the spec by name if it never does.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="description">The condition as written at the call site; supplied by the compiler.</param>
    /// <returns>A <see cref="Task"/> that completes once the condition holds.</returns>
    /// <exception cref="TimeoutException">Thrown when the condition does not hold within the deadline.</exception>
    protected async Task WaitFor(Func<bool> condition, [CallerArgumentExpression(nameof(condition))] string? description = null)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var next = _signals.Next;
            if (condition())
            {
                return;
            }

            try
            {
                await next.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for: {description}");
            }
        }
    }
}

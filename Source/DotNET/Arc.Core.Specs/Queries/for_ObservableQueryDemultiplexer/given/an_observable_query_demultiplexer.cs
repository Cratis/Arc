// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
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
    protected IOptions<ArcOptions> _arcOptions;
    protected ILogger<ObservableQueryDemultiplexer> _logger;
    protected ObservableQueryDemultiplexer _hub;

    void Establish()
    {
        _queryPipeline = Substitute.For<IQueryPipeline>();
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        _hostApplicationLifetime.ApplicationStopping.Returns(CancellationToken.None);
        _arcOptions = Options.Create(new ArcOptions());
        _logger = Substitute.For<ILogger<ObservableQueryDemultiplexer>>();
        _hub = new ObservableQueryDemultiplexer(
            _queryPipeline,
            _queryContextManager,
            _httpRequestContextAccessor,
            _hostApplicationLifetime,
            _arcOptions,
            _logger);
    }

    /// <summary>
    /// Polls the condition until it returns <see langword="true"/> or a 2-second timeout elapses.
    /// </summary>
    /// <param name="condition">The condition to poll.</param>
    /// <returns>A <see cref="Task"/> that completes when the condition is met or the timeout expires.</returns>
    protected static async Task WaitFor(Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow < timeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }
    }
}

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
}

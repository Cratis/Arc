// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.given;

public class an_observable_query_handler : Specification
{
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected ILogger<ObservableQueryHandler> _logger;
    protected ObservableQueryHandler _handler;
    protected QueryContext _queryContext;

    void Establish()
    {
        _queryContext = new QueryContext("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_queryContext);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = Substitute.For<ILogger<ObservableQueryHandler>>();

        _handler = new ObservableQueryHandler(_queryContextManager, _serviceProvider, _logger);
    }
}

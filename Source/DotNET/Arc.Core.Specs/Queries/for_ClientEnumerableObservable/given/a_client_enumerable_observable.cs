// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.given;

public class a_client_enumerable_observable : Specification
{
    protected QueryContext _queryContext;
    protected TestAsyncEnumerable _enumerable;
    protected IReadModelInterceptors _readModelInterceptors;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected IObservableQueryEmissionGuards _emissionGuards;
    protected ILogger<IClientObservable> _logger;
    protected ClientEnumerableObservable<TestData> _clientEnumerableObservable;

    void Establish()
    {
        _queryContext = new QueryContext("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _enumerable = new TestAsyncEnumerable();
        _readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _emissionGuards = Substitute.For<IObservableQueryEmissionGuards>();
        _logger = Substitute.For<ILogger<IClientObservable>>();

        _clientEnumerableObservable = new ClientEnumerableObservable<TestData>(
            _queryContext,
            _enumerable,
            _readModelInterceptors,
            _webSocketConnectionHandler,
            _emissionGuards,
            _logger);
    }

    public record TestData(string Value);

    public class TestAsyncEnumerable : IAsyncEnumerable<TestData>
    {
        readonly List<TestData> _items = [new("First"), new("Second"), new("Third")];

        public async IAsyncEnumerator<TestData> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
            {
                await Task.Yield();
                yield return item;
            }
        }
    }
}

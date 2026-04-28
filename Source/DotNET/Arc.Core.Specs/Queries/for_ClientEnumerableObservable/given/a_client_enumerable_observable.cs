// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.given;

public class a_client_enumerable_observable : Specification
{
    protected TestAsyncEnumerable _enumerable;
    protected IReadModelInterceptors _readModelInterceptors;
    protected IServiceProvider _serviceProvider;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected ILogger<IClientObservable> _logger;
    protected ClientEnumerableObservable<TestData> _clientEnumerableObservable;

    void Establish()
    {
        _enumerable = new TestAsyncEnumerable();
        _readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _logger = Substitute.For<ILogger<IClientObservable>>();

        _clientEnumerableObservable = new ClientEnumerableObservable<TestData>(
            _enumerable,
            _readModelInterceptors,
            _serviceProvider,
            _webSocketConnectionHandler,
            _logger);
    }

    protected record TestData(string Value);

    protected class TestAsyncEnumerable : IAsyncEnumerable<TestData>
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.given;

public class a_client_enumerable_observable : Specification
{
    protected TestAsyncEnumerable _enumerable;
    protected System.Text.Json.JsonSerializerOptions _jsonOptions;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected ILogger<IClientObservable> _logger;
    protected ClientEnumerableObservable<TestData> _clientEnumerableObservable;

    void Establish()
    {
        _enumerable = new TestAsyncEnumerable();
        _jsonOptions = new System.Text.Json.JsonSerializerOptions();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _logger = Substitute.For<ILogger<IClientObservable>>();

        _clientEnumerableObservable = new ClientEnumerableObservable<TestData>(
            _enumerable,
            _jsonOptions,
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

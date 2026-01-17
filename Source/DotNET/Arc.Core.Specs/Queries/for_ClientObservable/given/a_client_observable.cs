// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientObservable.given;

public class a_client_observable : Specification
{
    protected QueryContext _queryContext;
    protected BehaviorSubject<TestData> _subject;
    protected System.Text.Json.JsonSerializerOptions _jsonOptions;
    protected IWebSocketConnectionHandler _webSocketConnectionHandler;
    protected IHostApplicationLifetime _hostApplicationLifetime;
    protected ILogger<ClientObservable<TestData>> _logger;
    protected ClientObservable<TestData> _clientObservable;

    void Establish()
    {
        _queryContext = new QueryContext("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _subject = new BehaviorSubject<TestData>(new TestData("Initial"));
        _jsonOptions = new System.Text.Json.JsonSerializerOptions();
        _webSocketConnectionHandler = Substitute.For<IWebSocketConnectionHandler>();
        _hostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
        _logger = Substitute.For<ILogger<ClientObservable<TestData>>>();

        _clientObservable = new ClientObservable<TestData>(
            _queryContext,
            _subject,
            _jsonOptions,
            _webSocketConnectionHandler,
            _hostApplicationLifetime,
            _logger);
    }

    public record TestData(string Value);
}

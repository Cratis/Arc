// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_valid_websocket_arguments : given.a_controller_query_request
{
    ReplaySubject<TestReadModel> _subject;
    List<object> _data;

    void Establish()
    {
        _webSocketFeature.IsWebSocketRequest.Returns(true);
        _subject = new ReplaySubject<TestReadModel>();
        _subject.OnNext(new("first"));
        _subject.OnNext(new("second"));
        _subject.OnCompleted();
        _executed.Result = new ObjectResult(_subject);
        _data = [];
        _connectionHandler.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _data.Add(call.Arg<QueryResult>().Data);
                return Task.FromResult<Exception?>(null);
            });
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, _next).WaitAsync(TimeSpan.FromSeconds(10));

    void Destroy() => _subject.Dispose();

    [Fact] void should_execute_the_action() => _next.Received(1).Invoke();
    [Fact] void should_stream_both_results() => _data.ShouldEqual([new TestReadModel("first"), new TestReadModel("second")]);
    [Fact] void should_accept_the_websocket() => _transportEvents.ShouldEqual(["accept"]);
    [Fact] void should_not_send_a_validation_rejection() => _frames.ShouldBeEmpty();
    [Fact] void should_not_short_circuit_mvc() => _actionContext.Result.ShouldBeNull();
    [Fact] void should_keep_http_success() => _httpContext.Response.StatusCode.ShouldEqual(200);
}

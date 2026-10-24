// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_subject_errors : given.a_guarded_client_observable
{
    QueryResult _result;
    Task _connection;

    void Establish() => _webSocketConnectionHandler
        .SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
        .Returns(call =>
        {
            _result = call.Arg<QueryResult>();
            return Task.FromResult<Exception?>(null);
        });

    async Task Because()
    {
        _connection = _observable.HandleConnection(_context);
        _subject.OnError(new InvalidOperationException("The source failed"));
        await _connection.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_send_one_result() => _webSocketConnectionHandler.Received(1).SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>());
    [Fact] void should_send_a_failed_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_send_the_exception_message() => _result.ExceptionMessages.ShouldContainOnly("The source failed");
    [Fact] void should_send_the_subscription_correlation_id() => _result.CorrelationId.ShouldEqual(_queryContext.CorrelationId);
    [Fact] void should_not_send_a_stack_trace() => _result.ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_complete_the_handler() => _connection.IsCompletedSuccessfully.ShouldBeTrue();
}

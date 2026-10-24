// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_sending_the_error_throws : given.a_guarded_client_observable
{
    Exception _error;
    Task _connection;

    void Establish() => _webSocketConnectionHandler
        .SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<Exception?>(new InvalidOperationException("The client is gone")));

    async Task Because()
    {
        _connection = _observable.HandleConnection(_context);
        _subject.OnError(new InvalidOperationException("The source failed"));
        _error = await Catch.Exception(() => _connection.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_log_the_send_failure() => _logger.ReceivedCalls().SelectMany(_ => _.GetArguments()).OfType<Exception>().Any(_ => _.Message == "The client is gone").ShouldBeTrue();
    [Fact] void should_not_rethrow_the_send_failure() => _error.ShouldBeNull();
    [Fact] void should_complete_the_handler() => _connection.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_attempt_only_one_error_send() => _webSocketConnectionHandler.Received(1).SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>());
}

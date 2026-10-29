// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_subject_errors_with_details_hidden : given.a_guarded_client_observable
{
    QueryResult _error;

    void Establish()
    {
        _context.RequestServices.GetService(typeof(IOptions<ArcOptions>)).Returns(Options.Create(new ArcOptions { ExposeExceptionDetails = false }));
        _webSocketConnectionHandler.SendMessage(Arg.Any<Http.IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _error = call.Arg<QueryResult>();
                return Task.FromResult<Exception?>(null);
            });
    }

    Task Because() => RunConnection(async () =>
    {
        _subject.OnError(new InvalidOperationException("Producer failed"));
        await WaitFor(() => _error is not null);
    });

    [Fact] void should_hide_the_subject_error() => _error.ExceptionMessages.ShouldEqual([ExceptionDetailRedactor.RedactedMessage]);
}

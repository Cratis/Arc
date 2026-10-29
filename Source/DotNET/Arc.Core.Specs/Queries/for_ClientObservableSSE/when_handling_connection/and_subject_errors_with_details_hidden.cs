// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_subject_errors_with_details_hidden : given.a_guarded_client_observable_sse
{
    void Establish() => _arcOptions.Value.ExposeExceptionDetails = false;

    Task Because() => RunConnection(async () =>
    {
        _subject.OnError(new InvalidOperationException("Producer failed"));
        await WaitFor(() => WrittenResults.Any());
    });

    [Fact] void should_hide_the_subject_error() => WrittenResults.Single().ExceptionMessages.ShouldEqual([ExceptionDetailRedactor.RedactedMessage]);
}

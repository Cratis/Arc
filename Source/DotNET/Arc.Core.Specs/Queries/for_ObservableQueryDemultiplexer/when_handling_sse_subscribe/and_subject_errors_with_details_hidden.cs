// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_subject_errors_with_details_hidden : given.a_guarded_sse_connection
{
    void Establish() => _arcOptions.Value.ExposeExceptionDetails = false;

    Task Because() => RunConnection(async () =>
    {
        _subject.OnError(new InvalidOperationException("Producer failed"));
        await WaitFor(() => HasErrorFor(FirstQueryId));
    });

    [Fact] void should_hide_the_subject_error() => HubMessages.Single(message => message.Type == ObservableQueryHubMessageType.Error && message.QueryId == FirstQueryId)
        .Payload!.ToString().ShouldEqual(ExceptionDetailRedactor.RedactedMessage);
}

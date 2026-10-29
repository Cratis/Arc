// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_a_custom_subject_throws_on_subscribe_with_details_hidden : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _arcOptions.Value.ExposeExceptionDetails = false;
        _streamingData = new FailingSubject();
    }

    Task Because() => RunConnection(() => WaitFor(() => HasErrorFor(FirstQueryId)));

    [Fact] void should_hide_the_subscription_error() => _sentMessages.Single(message => message.Type == ObservableQueryHubMessageType.Error && message.QueryId == FirstQueryId)
        .Payload!.ToString().ShouldEqual(ExceptionDetailRedactor.RedactedMessage);

    sealed class FailingSubject : ISubject<IEnumerable<string>>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(IEnumerable<string> value) { }
        public IDisposable Subscribe(IObserver<IEnumerable<string>> observer) => throw new NotSupportedException("Cannot observe subject");
    }
}

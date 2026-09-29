// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

/// <summary>
/// A query's own <see cref="ISubject{T}"/> can fail synchronously when subscribed to. The client is told the
/// subscription could not be prepared, and the connection keeps serving.
/// </summary>
public class and_a_custom_subject_throws_on_subscribe : given.a_guarded_websocket_connection
{
    void Establish() => _streamingData = new FailingSubject();

    async Task Because() => await RunConnection(() => WaitFor(() => HasErrorFor(FirstQueryId)));

    [Fact] void should_send_an_error_for_the_subscription() => HasErrorFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_send_any_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();

    sealed class FailingSubject : ISubject<IEnumerable<string>>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(IEnumerable<string> value)
        {
        }

        public IDisposable Subscribe(IObserver<IEnumerable<string>> observer) =>
            throw new NotSupportedException("the subject cannot be observed");
    }
}

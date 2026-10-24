// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_subject_errors : given.a_guarded_client_observable_sse
{
    Task _connection;

    async Task Because()
    {
        _connection = _observable.HandleConnection(_context);
        _subject.OnError(new InvalidOperationException("The source failed"));
        await _connection.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_send_one_result() => WrittenResults.Count().ShouldEqual(1);
    [Fact] void should_send_a_failed_result() => WrittenResults.Single().IsSuccess.ShouldBeFalse();
    [Fact] void should_send_the_exception_message() => WrittenResults.Single().ExceptionMessages.ShouldContainOnly("The source failed");
    [Fact] void should_send_the_subscription_correlation_id() => WrittenResults.Single().CorrelationId.ShouldEqual(_queryContext.CorrelationId);
    [Fact] void should_not_send_a_stack_trace() => WrittenResults.Single().ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_complete_the_handler() => _connection.IsCompletedSuccessfully.ShouldBeTrue();
}

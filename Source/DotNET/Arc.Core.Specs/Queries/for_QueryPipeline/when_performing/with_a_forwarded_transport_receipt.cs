// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_forwarded_transport_receipt : given.a_query_pipeline
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _contextReceipt;
    DateTimeOffset? _after;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received, _received.AddMinutes(1));
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            _contextReceipt = call.Arg<QueryContext>().ReceivedAt;
            return QueryResult.Success(CorrelationId.New());
        });
    }

    async Task Because()
    {
        using (OperationContextScope.Begin(_serviceProvider))
        {
            using var dispatch = OperationContextScope.ForwardTransportReceipt();
            await _pipeline.Perform("Test.Receipt", QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider);
        }
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_keep_the_receipt_captured_before_the_public_entry() => _contextReceipt.ShouldEqual(_received);
    [Fact] void should_restore_the_receipt_after_dispatch() => _after.ShouldBeNull();
}

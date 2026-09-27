// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_an_operation_receipt : given.a_query_pipeline
{
    readonly DateTimeOffset _now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    QueryContext _context = null!;
    DateTimeOffset? _filterReceipt;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _serviceProvider.GetService(typeof(IOperationContextAccessor)).Returns(new OperationContextAccessor());
        var name = new FullyQualifiedQueryName("Test.Receipt");
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(name, out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            _context = call.Arg<QueryContext>();
            _filterReceipt = ((IOperationContextAccessor)_context.ServiceProvider!.GetService(typeof(IOperationContextAccessor))!).ReceivedAt;
            return QueryResult.Success(CorrelationId.New());
        });
    }

    async Task Because() => await _pipeline.Perform("Test.Receipt", QueryArguments.Empty, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_put_the_receipt_on_the_query_context() => _context.ReceivedAt.ShouldEqual(_now);
    [Fact] void should_expose_the_same_receipt_to_query_validators() => _filterReceipt.ShouldEqual(_now);
}

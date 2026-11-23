// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_an_execution_aware_availability_gate : given.a_query_pipeline
{
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    AvailabilityInvocation _invocation;
    QueryResult _result;

    void Establish()
    {
        var gate = Substitute.For<IAvailabilityExecutionGate>();
        gate.IsGated(typeof(object)).Returns(true);
        gate.Decide(Arg.Any<AvailabilityInvocation>(), _serviceProvider, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _invocation = call.Arg<AvailabilityInvocation>();
            return new AvailabilityDecision(AvailabilityVerdict.Unknown, []);
        });
        _serviceProvider.GetService(typeof(IAvailabilityGate)).Returns(gate);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_receipt);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter("count", typeof(int))]));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>()!).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(call => new Filters.AvailabilityFilter(_queryPerformerProviders).OnPerform(call.Arg<QueryContext>()));
    }

    async Task Because() => _result = await _pipeline.Perform("Test.Count", new QueryArguments { ["count"] = "42" }, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_pass_the_coerced_argument() => ((QueryArguments)_invocation.Arguments)["count"].ShouldEqual(42);
    [Fact] void should_pass_the_boundary_receipt() => _invocation.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_fail_closed() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_run_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
}

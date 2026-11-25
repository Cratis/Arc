// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;

namespace Cratis.Arc.Availability.for_AvailabilityContextValueSource;

public class when_a_query_gate_and_performer_read_the_same_value : given.an_invocation_host
{
    object _value;
    object _copiedValue;
    AvailabilityDecision _listing;

    async Task Because()
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Type.Returns(typeof(UngatedCommand));
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>()!).Returns(call =>
        {
            call[1] = performer;
            return true;
        });
        var context = new QueryContext(
            "Test.Selected",
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { ["vesselId"] = "allowed" },
            ServiceProvider: _scope.ServiceProvider)
        {
            ReceivedAt = Receipt
        };
        using var operation = OperationContextScope.Begin(_scope.ServiceProvider);
        await new Queries.Filters.AvailabilityFilter(providers).OnPerform(context);
        _value = await context.ProvidedValues.GetAsync("selected");
        _copiedValue = await (context with { Dependencies = [new object()] }).ProvidedValues.GetAsync("selected");
        _listing = await _gate.Decide(typeof(UngatedCommand), _scope.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_read_the_gate_value() => _value.ShouldEqual("allowed");
    [Fact] void should_keep_the_value_after_dependency_binding() => _copiedValue.ShouldEqual("allowed");
    [Fact] void should_not_supply_an_instance_to_metadata_even_during_execution() => _listing.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_resolve_once_for_execution_and_once_for_metadata() => _calls.ShouldEqual(2);
}

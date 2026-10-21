// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.when_creating;

public class with_an_occupied_generated_target : given.a_generated_aggregate_target
{
    void Establish() => _sequence.HasEventsFor(_id).Returns(true);

    async Task Because() => _error = await Catch.Exception(() => InInvocation(() => _factory.Get<NewAggregate>(_id)));

    [Fact] void should_reject_acquisition() => _error.ShouldBeOfExactType<GeneratedIdentifierCollision>();
    [Fact] void should_not_create_a_mutator_or_rehydrate() => _mutatorFactory.DidNotReceive().Create<NewAggregate>(Arg.Any<IAggregateRootContext>());
    [Fact] void should_classify_the_collision_for_the_pipeline() => CommandResult.FromException(_context.CorrelationId, _error!).ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_name_the_collision_for_the_pipeline() => CommandResult.FromException(_context.CorrelationId, _error!).ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_not_expose_the_identifier_in_the_error() => _error!.Message.ShouldNotContain(_id.Value);
}

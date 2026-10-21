// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class with_a_guarded_generated_target : Commands.given.a_generated_target
{
    AggregateRootMutation _mutation;
    AggregateRootContext _aggregateContext;
    IAggregateRootMutator _mutator;
    AggregateRootCommitResult _commit;

    void Establish()
    {
        var sequence = Substitute.For<IEventSequence>();
        sequence.Id.Returns(EventSequenceId.Log);
        _mutator = Substitute.For<IAggregateRootMutator>();
        _aggregateContext = new(new EventSourceType("narrowed-source"), _id, new EventStreamType("narrowed-stream"), new EventStreamId("narrowed-id"), sequence, new AggregateRoot(), _unit, EventSequenceNumber.First, new EventSequenceNumber(12))
        {
            FreshIdentifiers = _freshness
        };
        _mutation = new(_aggregateContext, _mutator, sequence);
        _unit.Violations = [new ConcurrencyViolation(_id, EventSequenceNumber.BeforeFirst, EventSequenceNumber.First)];
    }

    async Task Because()
    {
        await _mutation.Apply(new Created("first"));
        _aggregateContext.TailEventSequenceNumber = new EventSequenceNumber(25);
        _aggregateContext.HasEvents = true;
        await _mutation.Apply(new Created("second"));
        _commit = await _mutation.Commit();
    }

    [Fact] void should_keep_immutable_before_first() => _enrolledScopes.Single().Value.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_not_inherit_a_narrowed_stream() => _enrolledScopes.Single().Value.EventStreamType.ShouldBeNull();
    [Fact] void should_not_inherit_a_narrowed_stream_id() => _enrolledScopes.Single().Value.EventStreamId.ShouldBeNull();
    [Fact] void should_not_inherit_a_narrowed_source_type() => _enrolledScopes.Single().Value.EventSourceType.ShouldBeNull();
    [Fact] void should_report_concurrency_failure() => _commit.IsSuccess.ShouldBeFalse();
    [Fact] void should_translate_collision_provenance() => Commands.AggregateRootCommitResultExtensions.ToCommandResult(_commit, _context.CorrelationId).ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_preserve_the_concurrency_category() => Commands.AggregateRootCommitResultExtensions.ToCommandResult(_commit, _context.CorrelationId).ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
}

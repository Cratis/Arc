// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.when_two_writers_create_the_same_aggregate;

/// <summary>
/// Both writers loaded the aggregate before it had any event. The first one's event is the very first event of the
/// whole log, at sequence number zero; a scope expecting sequence number zero would take the second writer's
/// stale commit as seeing that event, so the scope has to expect no event at all.
/// </summary>
public class and_the_second_writer_commits_after_the_first : given.two_writers_creating_the_same_aggregate
{
    AppendResult _firstResult;
    AppendResult _secondResult;

    async Task Because()
    {
        _firstResult = await Append(_firstWritersScope);
        _secondResult = await Append(_secondWritersScope);
    }

    [Fact] void should_expect_no_matching_event_for_a_new_aggregate() => _firstWritersScope.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_let_the_first_writer_create_the_aggregate() => _firstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_validated_the_first_writers_commit() => _firstResult.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_reject_the_second_writers_stale_commit() => _secondResult.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_not_let_the_second_writer_succeed() => _secondResult.IsSuccess.ShouldBeFalse();
}

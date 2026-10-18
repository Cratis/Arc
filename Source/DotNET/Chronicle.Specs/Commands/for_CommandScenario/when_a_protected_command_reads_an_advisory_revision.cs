// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_a_protected_command_reads_an_advisory_revision : given.an_advisory_revision
{
    CommandScenario<ReadAdvisoryRevisionWithProtection> _scenario;

    async Task Establish()
    {
        _scenario = new CommandScenario<ReadAdvisoryRevisionWithProtection>().UseDecisionReads();
        await Seed(_scenario);
    }

    async Task Because() => _result = await _scenario.Execute(new ReadAdvisoryRevisionWithProtection(_source));

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_pass_the_read_guard_without_a_revision_conflict() => _result.ShouldBeSuccessful();
    [Fact] void should_append_the_command_event() => _scenario.AppendedEvents.Count.ShouldEqual(1);
    [Fact] async Task should_keep_the_global_sequence_number() => await _scenario.ShouldHaveAppendedEvent<ReadAdvisoryRevisionWithProtection, RevisionObserved>(_source, _ => _.Revision == 2);
    [Fact] async Task should_keep_the_original_occurrence_time() => await _scenario.ShouldHaveAppendedEvent<ReadAdvisoryRevisionWithProtection, RevisionObserved>(_source, _ => _.Occurred == _occurred);
    [Fact] async Task should_match_the_protected_fold() => await _scenario.ShouldHaveAppendedEvent<ReadAdvisoryRevisionWithProtection, RevisionObserved>(_source, _ => _.MatchesGuard);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_an_advisory_read_model_is_read_in_decision_mode : given.an_advisory_revision
{
    CommandScenario<ReadAdvisoryRevision> _scenario;

    async Task Establish()
    {
        _scenario = new CommandScenario<ReadAdvisoryRevision>().UseDecisionReads();
        await Seed(_scenario);
    }

    async Task Because() => _result = await _scenario.Execute(new ReadAdvisoryRevision(_source));

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_commit_without_a_revision_conflict() => _result.ShouldBeSuccessful();
    [Fact] void should_append_the_command_event() => _scenario.AppendedEvents.Count.ShouldEqual(1);
    [Fact] async Task should_keep_the_global_sequence_number() => await _scenario.ShouldHaveAppendedEvent<ReadAdvisoryRevision, RevisionObserved>(_source, _ => _.Revision == 2);
    [Fact] async Task should_keep_the_original_occurrence_time() => await _scenario.ShouldHaveAppendedEvent<ReadAdvisoryRevision, RevisionObserved>(_source, _ => _.Occurred == _occurred);
}

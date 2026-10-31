// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_decision_mode;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_appending_concurrently_without_guarded_reads : Specification
{
    readonly EventSourceId _source = EventSourceId.New();
    CommandScenario<Ordinary> _scenario;
    CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _scenario.AppendConcurrently(_source, new DecisionStateChanged());
    }

    async Task Because() => _result = await _scenario.Execute(new(_source), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_preserve_execution_without_a_guard() => _result.ShouldBeSuccessful();
    [Fact] async Task should_commit_the_owner_event() => await _scenario.ShouldHaveAppendedEvent<Ordinary, DecisionFinished>(_source);
    [Fact] async Task should_append_the_competing_event() => await _scenario.EventLog.ShouldHaveAppendedEvent<DecisionStateChanged>(_source);

    async Task Destroy() => await _scenario.DisposeAsync();
}

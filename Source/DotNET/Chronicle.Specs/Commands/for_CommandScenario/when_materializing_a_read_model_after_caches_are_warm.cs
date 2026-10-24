// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_materializing_a_read_model_after_caches_are_warm : Specification
{
    CommandScenario<SettleLedger> _scenario;
    EventSourceId _source;

    async Task Establish()
    {
        await using var warmup = new CommandScenario<AutomaticRead>();
        (await warmup.Validate(new AutomaticRead(EventSourceId.New()))).IsSuccess.ShouldBeTrue();
        _scenario = new CommandScenario<SettleLedger>().UseDecisionReads();
        _source = EventSourceId.New();
        _scenario.Given.ForEventSource(_source).Events(new LedgerCredited(10m), new LedgerDebited(3m), new LedgerCredited(5m));
    }

    async Task Because() => (await _scenario.Execute(new SettleLedger(_source))).ShouldBeSuccessful();

    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] Task should_reduce_the_seeded_history() =>
        _scenario.ShouldHaveAppendedEvent<SettleLedger, LedgerSettled>(_source, _ => _.Balance == 12m);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransaction;

public class when_a_protected_unit_was_rolled_back
{
    [Fact]
    public async Task should_refuse_every_returned_event_fallback()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        log.Id.Returns(EventSequenceId.Log);
        unit.AddDecisionRead(DecisionFixtures.Protected<object>("source"));
        await unit.Rollback();
        var types = Substitute.For<IEventTypes>();
        types.HasFor(typeof(Event)).Returns(true);
        var strategies = Substitute.For<IConcurrencyScopeStrategies>();
        strategies.GetFor(log).Returns(Substitute.For<IConcurrencyScopeStrategy>());
        var correlation = CorrelationId.New();
        var values = new CommandContextValues { { WellKnownCommandContextKeys.EventSourceId, (EventSourceId)"source" } };
        var context = new CommandContext(correlation, typeof(Command), new Command(), [], values);
        var @event = new Event();
        var wrapped = new EventForEventSourceId("source", @event);
        CommandTransaction.Current = unit;
        try
        {
            await Assert.ThrowsAsync<ReturnedEventsCannotBeAppendedOutsideDecisionTransaction>(() => new SingleEventCommandResponseValueHandler(log, types, strategies).Handle(context, @event));
            await Assert.ThrowsAsync<ReturnedEventsCannotBeAppendedOutsideDecisionTransaction>(() => new SingleEventForEventSourceIdCommandResponseValueHandler(log, types, strategies).Handle(context, wrapped));
            await Assert.ThrowsAsync<ReturnedEventsCannotBeAppendedOutsideDecisionTransaction>(() => new EventsCommandResponseValueHandler(log, types, strategies).Handle(context, new[] { @event }));
            await Assert.ThrowsAsync<ReturnedEventsCannotBeAppendedOutsideDecisionTransaction>(() => new EventsForEventSourceIdCommandResponseValueHandler(log, types, strategies).Handle(context, new[] { wrapped }));
            await Assert.ThrowsAsync<ReturnedEventsCannotBeAppendedOutsideDecisionTransaction>(() => new EventsWithConcurrencyScopesCommandResponseValueHandler(log)
                .Handle(context, new EventsWithConcurrencyScopes([wrapped], [])));
            Assert.False(CommandTransaction.TryGetActive(out _));
        }
        finally
        {
            CommandTransaction.Current = null;
        }
    }

    public class Command;
    public class Event;
}

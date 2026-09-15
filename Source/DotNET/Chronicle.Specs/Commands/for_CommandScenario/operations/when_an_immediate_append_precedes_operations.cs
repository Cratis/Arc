// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public class when_an_immediate_append_precedes_operations : Specification
{
    CommandScenario<AppendThenReserve> _scenario;
    IOnboardingReservations _reservations;
    EventSourceId _partner;
    CommandResult _result;

    void Establish()
    {
        _partner = EventSourceId.New();
        _reservations = Substitute.For<IOnboardingReservations>();
        _scenario = new();
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() => _result = await _scenario.Execute(new(_partner));
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_reject_operations_after_early_commit() => _result.ShouldNotBeSuccessful();
    [Fact] void should_start_no_operations() => _scenario.ShouldHaveNoOperationInvocations();
    [Fact] void should_report_the_already_committed_fact() => _result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.Committed);
    [Fact] void should_not_invent_reversal_for_immediate_writes() => _reservations.DidNotReceive().Cancel(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_leave_the_immediate_fact_committed() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();

    [Command]
    public record AppendThenReserve(EventSourceId EventSourceId)
    {
        public async Task<CommandOperations> Handle(IEventLog eventLog)
        {
            await eventLog.Append(EventSourceId, new PartnerAdminInvited(EventSourceId));
            return [new ReserveOnboardingCapacity(Guid.Empty)];
        }
    }
}

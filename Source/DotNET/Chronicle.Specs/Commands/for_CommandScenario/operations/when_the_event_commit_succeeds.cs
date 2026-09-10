// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public class when_the_event_commit_succeeds : Specification
{
    CommandScenario<StartPartnerOnboardingWithReservation> _scenario;
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

    async Task Because() => _result = await _scenario.Execute(new(_partner, "ORG-OPERATIONS-SUCCESS", Guid.NewGuid()));
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_report_committed_facts() => _result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.Committed);
    [Fact] void should_not_reverse_the_reservation() => _reservations.DidNotReceive().Cancel(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_persist_the_returned_event() => (await _scenario.EventScenario.EventLog.HasEventsFor(_partner)).ShouldBeTrue();
}

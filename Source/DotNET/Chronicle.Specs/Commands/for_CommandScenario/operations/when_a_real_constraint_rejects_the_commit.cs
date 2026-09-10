// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.operations;

public class when_a_real_constraint_rejects_the_commit : Specification
{
    CommandScenario<StartPartnerOnboardingWithReservation> _scenario;
    IOnboardingReservations _reservations;
    EventSourceId _newPartner;
    Guid _reservationKey;
    CommandResult _result;
    List<string> _calls;

    async Task Establish()
    {
        _newPartner = EventSourceId.New();
        _reservationKey = Guid.NewGuid();
        _calls = [];
        _reservations = Substitute.For<IOnboardingReservations>();
        _reservations.Reserve(_reservationKey, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _calls.Add("reserve");
            return Task.CompletedTask;
        });
        _reservations.Cancel(_reservationKey, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _calls.Add("cancel");
            return Task.CompletedTask;
        });
        _scenario = new();
        _scenario.Services.AddSingleton(_reservations);
        await _scenario.EventScenario.Given.ForEventSource(EventSourceId.New()).Events(new PartnerOnboardingStarted("ORG-OPERATIONS"));
    }

    async Task Because() => _result = await _scenario.Execute(new(_newPartner, "ORG-OPERATIONS", _reservationKey));
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_fail_the_actual_constraint_check() => _result.ValidationResults.Any(result => result.Reason == ValidationResultReason.ConstraintViolation).ShouldBeTrue();
    [Fact] void should_not_claim_command_success() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_executed_before_the_deferred_commit() => _scenario.ShouldHaveExecutedOperation<ReserveOnboardingCapacity>();
    [Fact] void should_compensate_after_known_rejection() => _scenario.ShouldHaveCompensatedOperation<ReserveOnboardingCapacity>();
    [Fact] void should_preserve_forward_then_recovery_order() => _calls.ShouldEqual(["reserve", "cancel"]);
    [Fact] void should_report_known_noncommit() => _result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.NotCommitted);
    [Fact] void should_report_callback_completion() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);
    [Fact] async Task should_leave_no_rejected_event_in_the_real_in_memory_log() => (await _scenario.EventScenario.EventLog.HasEventsFor(_newPartner)).ShouldBeFalse();
}

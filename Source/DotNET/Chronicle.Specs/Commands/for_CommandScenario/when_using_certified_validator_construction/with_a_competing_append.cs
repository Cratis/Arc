// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_a_competing_append : given.a_certified_command
{
    CommandResult _result;

    void Establish() => _scenario.AppendConcurrently(_command.EventSourceId, new when_using_decision_mode.DecisionStateChanged());

    async Task Because()
    {
        Build();

        // Validation has no commit; the validator's read must still enroll when execution starts a new invocation.
        (await _pipeline.Validate(_command)).ShouldBeSuccessful();
        _scenario.AppendedEvents.ShouldBeEmpty();
        _result = await _pipeline.Execute(_command);
    }

    [Fact] void should_reject_the_owner_commit_after_a_validator_read_conflicts() => _result.ShouldHaveValidationErrorBecauseOf(Validation.ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_not_append_the_owner_event() => _scenario.AppendedEvents.ShouldBeEmpty();
}

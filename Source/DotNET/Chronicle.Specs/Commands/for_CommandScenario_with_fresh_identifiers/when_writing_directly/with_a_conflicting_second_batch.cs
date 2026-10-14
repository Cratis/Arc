// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_a_conflicting_second_batch : given.a_direct_write_scenario<ConflictingRepeatedWrite>
{
    readonly ConflictingRepeatedWrite _command = new(Batch: true);
    async Task Because() => await Execute(_command);

    [Fact] void should_reject_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_translate_the_batch_preflight_conflict() => _result.ExceptionMessages.ShouldContainOnly(new ConflictingConcurrencyScopes().Message);
    [Fact] void should_reject_before_the_second_callback() => _command.EnrollmentCallbacks.ShouldEqual(1);
    [Fact] void should_roll_back_the_first_event() => _after.ShouldEqual(_before);
}

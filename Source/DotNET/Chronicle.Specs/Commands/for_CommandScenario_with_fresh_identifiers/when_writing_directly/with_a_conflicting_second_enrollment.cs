// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_a_conflicting_second_enrollment : given.a_direct_write_scenario<ConflictingRepeatedWrite>
{
    readonly ConflictingRepeatedWrite _command = new();
    async Task Because() => await Execute(_command);

    [Fact] void should_reject_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_translate_the_pre_contributor_conflict() => _result.ExceptionMessages.ShouldContainOnly(new ConflictingConcurrencyScopes().Message);
    [Fact] void should_reject_before_the_second_callback() => _command.EnrollmentCallbacks.ShouldEqual(1);
    [Fact] void should_roll_back_the_first_enrollment_too() => _after.ShouldEqual(_before);
    [Fact] void should_not_expose_the_generated_identifier() => _result.ExceptionMessages.ShouldEachConformTo(message => !message.Contains(_id.Value.ToString(), StringComparison.Ordinal));
}

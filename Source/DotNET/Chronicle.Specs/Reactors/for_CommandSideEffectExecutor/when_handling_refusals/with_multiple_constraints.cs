// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_multiple_constraints : given.a_refused_invocation
{
    void Establish()
    {
        _commandResult.ValidationResults =
        [
            ValidationResult.Error("first", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "First"),
            ValidationResult.Error("second", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "Second")
        ];
        _invocation = Invoke.Command(_command).OnRefused(Refused.ByConstraint("Second"), Record);
    }

    async Task Because() => _result = await _executor.Execute([_invocation, _later], _context, _store);

    [Fact] void should_match_a_constraint_other_than_the_first() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_retain_the_first_name() => _received!.ConstraintName.ShouldEqual("First");
    [Fact] void should_retain_every_name() => _received!.ConstraintNames.ShouldEqual(["First", "Second"]);
    [Fact] void should_record_all_constraint_messages() => _appended.Single().Event.ShouldEqual(new RefusalRecorded("first\nsecond"));
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}

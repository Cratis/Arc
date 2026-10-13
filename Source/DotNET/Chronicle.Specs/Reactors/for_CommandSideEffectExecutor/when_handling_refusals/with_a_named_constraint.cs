// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_a_named_constraint : given.a_refused_invocation
{
    void Establish()
    {
        _commandResult.ValidationResults = [ValidationResult.Error("taken", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueSeat")];
        _invocation = Invoke.Command(_command)
            .OnRefused(Refused.ByConstraint("Other"), _ => null)
            .OnRefused(Refused.ByConstraint("UniqueSeat"), Record);
    }
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_match_the_named_constraint() => _received!.ConstraintName.ShouldEqual("UniqueSeat");
    [Fact] void should_classify_a_constraint() => _received!.Kind.ShouldEqual(CommandRefusalKind.Constraint);
    [Fact] void should_append_its_message() => _appended.Single().Event.ShouldEqual(new RefusalRecorded("taken"));
}

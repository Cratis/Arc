// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_any_constraint : given.a_refused_invocation
{
    void Establish()
    {
        _commandResult.ValidationResults = [ValidationResult.Error("taken", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueSeat")];
        _invocation = Invoke.Command(_command).OnRefused(Refused.ByConstraint(), Record);
    }
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass_the_constraint_identity() => _received!.ConstraintName.ShouldEqual("UniqueSeat");
}

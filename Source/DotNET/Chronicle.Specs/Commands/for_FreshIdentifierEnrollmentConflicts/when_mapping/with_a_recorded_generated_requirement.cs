// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentConflicts.when_mapping;

public class with_a_recorded_generated_requirement : given.a_recorded_guard
{
    void Because()
    {
        CommandTransaction.Current = _receivingUnit;
        _handled = _mapper.TryMap(_context, _conflict, out _mapped);
    }

    [Fact] void should_classify_the_typed_conflict() => _handled.ShouldBeTrue();
    [Fact] void should_return_arcs_redacted_failure() => _mapped!.ExceptionMessages.ShouldContainOnly(new ConflictingConcurrencyScopes().Message);
    [Fact] void should_preserve_the_correlation_id() => _mapped!.CorrelationId.ShouldEqual(_context.CorrelationId);
    [Fact] void should_not_turn_a_conflict_into_success() => _mapped!.IsSuccess.ShouldBeFalse();
}

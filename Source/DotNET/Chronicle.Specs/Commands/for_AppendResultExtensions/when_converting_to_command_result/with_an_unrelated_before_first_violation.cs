// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_AppendResultExtensions.when_converting_to_command_result;

public class with_an_unrelated_before_first_violation : Commands.given.a_generated_target
{
    void Because() => _result = AppendResult.Failed(_context.CorrelationId, new ConcurrencyViolation(new("not-generated"), EventSequenceNumber.BeforeFirst, EventSequenceNumber.First)).ToCommandResult(_context, EventSequenceId.Log);

    [Fact] void should_preserve_the_concurrency_category() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_not_invent_generated_provenance() => _result.ValidationResults.Single().ReasonDetail.ShouldBeNull();
}

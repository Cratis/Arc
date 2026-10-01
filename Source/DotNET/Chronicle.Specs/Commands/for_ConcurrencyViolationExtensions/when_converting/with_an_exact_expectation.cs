// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyViolationExtensions.when_converting;

public class with_an_exact_expectation : Specification
{
    ValidationResult _result;

    void Because() => _result = new ConcurrencyViolation("account-42", 17UL, 18UL).ToValidationResult();

    [Fact] void should_describe_both_positions() => _result.Message.ShouldEqual(
        "Event source 'account-42' has new events since the command read it: expected events up to sequence number 17, but it has events up to sequence number 18. Read it again and resubmit.");
}

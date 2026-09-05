// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_a_command_with_a_provide_method;

public class when_handling_directly : given.a_loan_approval_command
{
    LoanApproved? _approved;
    LoanApproved? _rejected;

    void Because()
    {
        _approved = new ApproveLoan("acme").Handle(new CreditScore(800));
        _rejected = new ApproveLoan("acme").Handle(new CreditScore(500));
    }

    [Fact] void should_approve_when_the_score_is_high_enough() => _approved.ShouldNotBeNull();
    [Fact] void should_reject_when_the_score_is_too_low() => _rejected.ShouldBeNull();
}

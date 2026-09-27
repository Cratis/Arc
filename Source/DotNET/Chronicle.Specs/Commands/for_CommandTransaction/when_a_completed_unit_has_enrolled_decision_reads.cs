// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransaction;

public class when_a_completed_unit_has_enrolled_decision_reads : Specification
{
    IUnitOfWork _unitOfWork;

    void Establish()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.IsCompleted.Returns(true);
        _unitOfWork.HasEnrolledDecisionReads.Returns(true);
    }

    void Because() => CommandTransaction.Current = _unitOfWork;

    [Fact] void should_refuse_the_immediate_append_even_after_completion() =>
        Assert.Throws<InvalidOperationException>(CommandTransaction.RefuseImmediateAppend);

    void Cleanup()
    {
        CommandTransaction.Current = Substitute.For<IUnitOfWork>();
        CommandTransaction.Current = null;
    }
}

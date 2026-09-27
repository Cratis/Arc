// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransaction;

public class when_a_completed_unit_has_enrolled_decision_reads : Specification
{
    IUnitOfWork _unitOfWork;

    void Establish()
    {
        var (_, _, unit) = DecisionFixtures.Transaction();
        unit.AddDecisionRead(DecisionFixtures.Protected<object>("source"));
        unit.Rollback().GetAwaiter().GetResult();
        _unitOfWork = unit;
    }

    [Fact]
    void should_refuse_the_immediate_append_even_after_completion()
    {
        CommandTransaction.Current = _unitOfWork;
        Assert.Throws<InvalidOperationException>(CommandTransaction.RefuseImmediateAppend);
    }

    void Cleanup()
    {
        CommandTransaction.Current = Substitute.For<IUnitOfWork>();
        CommandTransaction.Current = null;
    }
}

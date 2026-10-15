// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_a_typed_source_reaches_chronicle : given.a_protected_read
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_guard_the_same_wire_key_regardless_of_the_source_clr_type(bool derivedSource)
    {
        await Seed();
        var unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(Cratis.Execution.CorrelationId.New());
        var owner = unit.ClaimDecisionReadCommitOwnership();
        using var frame = unit.EnforceReadProtection();
        await _store.GetDecisionReads().GetFor<BookStock>(unit, (ReadModelKey)_book.Value);
        var source = derivedSource ? _book : new EventSourceId(_book.Value);
        unit.AddEventWithOptions(EventSequenceId.Log, source, new BookBorrowed(_member), Causation.Unknown(), new()
        {
            Origin = ConcurrencyScopeOrigin.Routing
        });
        await unit.CommitAsOwner(owner);
        Assert.True(unit.IsSuccess, string.Join(Environment.NewLine, unit.GetAppendErrors().Select(error => error.Value)));
        unit.GetAppendConditions().Count.ShouldEqual(1);
    }
}

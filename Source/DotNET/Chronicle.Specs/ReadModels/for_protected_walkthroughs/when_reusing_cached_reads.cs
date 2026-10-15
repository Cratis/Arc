// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_reusing_cached_reads : given.a_protected_read
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_recheck_staged_overlap_before_exposing_a_cached_model(bool nested)
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var parameter = typeof(Borrow).GetMethod(nameof(Borrow.Handle))!.GetParameters()[0];
            Resolve(parameter).ShouldNotBeNull();
            _store.EventLog.TryEnrollForCommand(_book, new StockChanged(2), _context, null).ShouldBeTrue();
            using var childPolicy = nested ? DecisionPolicyForSpecs.Begin(typeof(Borrow)) : null;
            if (nested) CommandDecisionReads.Begin(typeof(Borrow));
            try
            {
                Catch.Exception(() => Resolve(parameter)).ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
            }
            finally
            {
                if (nested) CommandDecisionReads.End();
            }
        }
        finally
        {
            _scope.End(_context);
        }
    }
}

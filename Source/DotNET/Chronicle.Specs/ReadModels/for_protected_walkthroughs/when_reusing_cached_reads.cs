// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
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

    [Fact]
    public async Task should_keep_guarding_a_read_a_nested_command_reused_from_the_unit()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        CommandResult result;
        try
        {
            var parameter = typeof(Borrow).GetMethod(nameof(Borrow.Handle))!.GetParameters()[0];
            var outer = Resolve(parameter);
            object? reused;
            using (DecisionPolicyForSpecs.Begin(typeof(Borrow)))
            {
                CommandDecisionReads.Begin(typeof(Borrow));
                try
                {
                    reused = Resolve(parameter);
                }
                finally
                {
                    CommandDecisionReads.End();
                }
            }
            reused.ShouldBeSame(outer);
            await Compete();
            Stage();
            result = await Complete();
        }
        finally
        {
            _scope.End(_context);
        }
        result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        var events = await _store.EventLog.GetFromSequenceNumber(Cratis.Chronicle.Events.EventSequenceNumber.First, _book);
        events.Count(@event => @event.Content is BookBorrowed).ShouldEqual(0);
    }
}

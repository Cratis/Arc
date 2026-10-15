// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_deferring_definition_routing : given.a_protected_read
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_merge_the_definitions_concurrency_with_the_read_at_commit(bool competitor)
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var definition = _store.EventSources.GetFor(typeof(Library));
            _context.Values[WellKnownCommandContextKeys.EventSource] = typeof(Library);
            _context.Values[WellKnownCommandContextKeys.EventStream] = "loans";
            _context.Values[WellKnownCommandContextKeys.ConcurrencyDimensions] = definition.ConcurrencyFor(definition.FindStream("loans"));
            Resolve(typeof(Borrow).GetMethod(nameof(Borrow.Handle))!.GetParameters()[0]).ShouldNotBeNull();
            var strategy = Substitute.For<IConcurrencyScopeStrategy>();
            var routing = CommandTransactionAppender.ResolveRouting(null, _context);
            var scope = await Commands.ConcurrencyScopeBuilder.BuildFor(_context, strategy, _book, routing);
            scope.Scope.ShouldBeNull();
            strategy.ReceivedCalls().ShouldBeEmpty();
            if (competitor) await Compete();
            Stage();
            var result = await Complete();
            result.IsSuccess.ShouldEqual(!competitor);
            if (competitor)
            {
                result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
                result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
            }
            else
            {
                var condition = _unit.GetAppendConditions().Single();
                condition.Label.ShouldEqual("book");
                condition.EventTypes.ShouldBeEmpty();
                condition.EventSourceId!.Value.ShouldEqual(_book.Value);
            }
        }
        finally
        {
            _scope.End(_context);
        }
    }
}

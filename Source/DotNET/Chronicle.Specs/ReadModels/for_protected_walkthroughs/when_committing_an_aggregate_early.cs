// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

/// <summary>
/// <c>return await aggregate.Commit()</c> after a protected read of another source changed: the caller gets the
/// read-changed refusal, not a generic concurrency violation, and nothing is written.
/// </summary>
public class when_committing_an_aggregate_early : given.a_protected_read
{
    [Fact]
    public async Task should_report_read_changed_and_write_nothing()
    {
        await Seed();
        var memberSource = (EventSourceId)_member.Value;
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        CommandResult result;
        try
        {
            ((BookStock)Resolve(typeof(Borrow).GetMethod(nameof(Borrow.Handle))!.GetParameters()[0])!).Available.ShouldEqual(1);
            await Compete();
            _store.EventLog.TryEnrollForCommand(memberSource, new LoanCountChanged(3), _context, null).ShouldBeTrue();
            var aggregate = Substitute.For<IAggregateRootContext>();
            aggregate.UnitOfWOrk.Returns(_unit);
            aggregate.EventSourceId.Returns(memberSource);
            var commit = await new AggregateRootMutation(aggregate, Substitute.For<IAggregateRootMutator>(), _store.EventLog).Commit();
            result = commit.ToCommandResult(_context.CorrelationId);
            await _scope.Complete(_context, result);
        }
        finally
        {
            _scope.End(_context);
        }

        result.ExceptionMessages.ShouldBeEmpty();
        var failure = result.ValidationResults.Single();
        failure.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
        failure.ReasonDetail.ShouldEqual("ReadChanged");
        failure.Message.ShouldEqual("BookStock 'book' changed after Borrow read it. Nothing from this command was written; submit it again.");
        var memberEvents = await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, memberSource);
        memberEvents.Count(@event => @event.Content is LoanCountChanged).ShouldEqual(1);
    }
}

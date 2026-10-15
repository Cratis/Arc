// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_protected_walkthroughs;

public class when_guarding_plain_models : given.a_protected_read
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_guard_the_validator_approval_and_share_its_instance_with_handle(bool competitor)
    {
        await Seed();
        var command = new Borrow(_book, _member);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(command);
        try
        {
            var validator = Validate(command);
            (await validator.ValidateAsync(command)).IsValid.ShouldBeTrue();
            var stock = (BookStock)Resolve(typeof(Borrow).GetMethod(nameof(Borrow.Handle))!.GetParameters()[0])!;
            stock.Available.ShouldEqual(1);
            stock.ShouldBeSame(validator.Stock);
            if (competitor) await Compete();
            Stage();
            var result = await Complete();
            result.IsSuccess.ShouldEqual(!competitor);
            if (competitor)
            {
                var failure = result.ValidationResults.Single();
                failure.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
                failure.ReasonDetail.ShouldEqual("ReadChanged");
                failure.Message.ShouldEqual("BookStock 'book' changed after Borrow read it. Nothing from this command was written; submit it again.");
                EndpointRouteHelperStatus(result);
            }
        }
        finally
        {
            _scope.End(_context);
        }
        var events = await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _book);
        events.Count(@event => @event.Content is BookBorrowed).ShouldEqual(competitor ? 0 : 1);
    }

    [Fact]
    public async Task should_write_nothing_when_the_validator_rejects()
    {
        await Seed(0);
        var before = await _store.EventLog.GetTailSequenceNumber();
        var command = new Borrow(_book, _member);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(command);
        try
        {
            (await Validate(command).ValidateAsync(command)).IsValid.ShouldBeFalse();
            var result = new CommandResult
            {
                CorrelationId = _context.CorrelationId,
                ValidationResults = [ValidationResult.Error("No copies left.")]
            };
            await _scope.Complete(_context, result);
        }
        finally
        {
            _scope.End(_context);
        }
        (await _store.EventLog.GetTailSequenceNumber()).ShouldEqual(before);
    }

    [Fact]
    public async Task should_enroll_two_guards_for_a_reference_found_by_convention()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var parameters = typeof(Borrow).GetMethod(nameof(Borrow.WithMember))!.GetParameters();
            ((BookStock)Resolve(parameters[0])!).Available.ShouldEqual(1);
            ((MemberLoans)Resolve(parameters[1])!).Count.ShouldEqual(2);
            Stage();
            (await Complete()).IsSuccess.ShouldBeTrue();
            _unit.GetAppendConditions().Select(condition => condition.Label).Order().ShouldEqual(["book", "member"]);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_keep_two_explicit_keys_of_the_same_model_separate()
    {
        await Seed();
        var other = new BookId("other");
        (await _store.EventLog.Append(other, new StockChanged(5))).IsSuccess.ShouldBeTrue();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Move));
        Begin(new Move(_book, _book, other));
        try
        {
            var parameters = typeof(Move).GetMethod(nameof(Move.Handle))!.GetParameters();
            ((BookStock)Resolve(parameters[0])!).Available.ShouldEqual(1);
            ((BookStock)Resolve(parameters[1])!).Available.ShouldEqual(5);
            Stage();
            (await Complete()).IsSuccess.ShouldBeTrue();
            _unit.GetAppendConditions().Select(condition => condition.Label).Order().ShouldEqual(["book", "other"]);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_not_promote_an_advisory_parameter_into_a_guard()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var parameters = typeof(Borrow).GetMethod(nameof(Borrow.Enrich))!.GetParameters();
            ((MemberLoans)Resolve(parameters[0])!).Count.ShouldEqual(2);
            ((BookStock)Resolve(parameters[1])!).Available.ShouldEqual(1);
            Stage();
            (await Complete()).IsSuccess.ShouldBeTrue();
            _unit.GetAppendConditions().Select(condition => condition.Label).ShouldEqual(["book"]);
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_refuse_raw_and_detached_reads_but_allow_explicit_advisory_reads()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var reader = _services.GetRequiredService<IDecisionReads>();
            (await Catch.Exception(async () => await _store.ReadModels.GetInstanceById<BookStock>((ReadModelKey)_book.Value)))
                .ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            var detached = await Catch.Exception(async () => await reader.GetDetached<BookStock>((ReadModelKey)_book.Value));
            detached.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
            new ReadProtectionCommandExceptionMapper().TryMap(_context, detached!, out var mapped).ShouldBeTrue();
            mapped!.ExceptionMessages.Single().ShouldContain("Borrow called GetDetached while read protection is On.");
            using var advisory = _unit.AllowReads(ReadAllowance.Advisory, "screen context only");
            (await reader.GetDetached<BookStock>((ReadModelKey)_book.Value)).Instance!.Available.ShouldEqual(1);
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_refuse_protected_parameters_but_allow_advisory_parameters_after_early_commit()
    {
        await Seed();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        Begin(new Borrow(_book, _member));
        try
        {
            var parameters = typeof(Borrow).GetMethod(nameof(Borrow.Enrich))!.GetParameters();
            Resolve(parameters[1]).ShouldNotBeNull();
            (await TransactionalCommandScope.CommitAggregate(_unit)).ShouldBeTrue();
            var failure = Catch.Exception(() => Resolve(parameters[1]));
            failure.ShouldBeOfExactType<ReadAfterProtectedCommit>();
            new ReadProtectionCommandExceptionMapper().TryMap(_context, failure!, out var mapped).ShouldBeTrue();
            mapped!.ExceptionMessages.Single().ShouldEqual("Borrow read BookStock after its changes were committed (aggregate Commit()). Read before committing, or return the result of Commit() last.");
            Resolve(parameters[0]).ShouldNotBeNull();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    [Fact]
    public async Task should_refold_preflight_validation_without_writing_or_reusing_its_read_for_execution()
    {
        await Seed();
        var command = new Borrow(_book, _member);
        _context = new(
            Cratis.Execution.CorrelationId.New(),
            typeof(Borrow),
            command,
            [],
            new() { [WellKnownCommandContextKeys.EventSourceId] = _book },
            ServiceProvider: _services);
        var validationBegin = typeof(CommandValidationExecution).GetMethod("Begin", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var before = await _store.EventLog.GetTailSequenceNumber();
        BookStock first;
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Borrow));
        using (var validation = (IDisposable)validationBegin.Invoke(null, [typeof(Borrow)])!)
        {
            first = Validate(command).Stock;
            first.Available.ShouldEqual(1);
        }
        (await _store.EventLog.GetTailSequenceNumber()).ShouldEqual(before);
        await Compete();
        Begin(command);
        try
        {
            var fresh = Validate(command).Stock;
            fresh.Available.ShouldEqual(0);
            ReferenceEquals(fresh, first).ShouldBeFalse();
        }
        finally
        {
            _scope.End(_context);
        }
    }

    static void EndpointRouteHelperStatus(CommandResult result) =>
        EndpointRouteHelper.GetStatusCode(result.IsSuccess, result.IsAuthorized, result.IsValid).ShouldEqual(System.Net.HttpStatusCode.BadRequest);
}

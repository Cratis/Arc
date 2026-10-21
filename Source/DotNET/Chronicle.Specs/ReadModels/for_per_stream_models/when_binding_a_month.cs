// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_binding_a_month : given.a_monthly_account
{
    [Theory]
    [InlineData("2026-10", false)]
    [InlineData("2026-11", true)]
    public async Task should_share_the_validator_fold_and_guard_only_its_month(string competitorMonth, bool succeeds)
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        var command = new Withdraw(_account, "2026-10", 100);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(command);
        var validator = Validate();
        (await validator.ValidateAsync(command)).IsValid.ShouldBeTrue();
        validator.Month!.Total.ShouldEqual(500);
        Resolve().ShouldBeSame(validator.Month);
        _services.GetRequiredService<MonthlyWithdrawals>().ShouldBeSame(validator.Month);
        _services.GetRequiredService<DecisionRead<MonthlyWithdrawals>>().Instance.ShouldBeSame(validator.Month);
        await Compete(competitorMonth);
        var result = await Complete();
        result.IsSuccess.ShouldEqual(succeeds);
        if (!succeeds)
        {
            result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
            result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        }
        _scope.End(_context);
        var events = await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _account);
        events.Count(item => item.Content is MoneyWithdrawn { Amount: 100 }).ShouldEqual(succeeds ? 1 : 0);
    }

    [Fact]
    public async Task should_reject_a_monthly_limit_before_staging()
    {
        await Append("2026-10", 9999);
        var command = new Withdraw(_account, "2026-10", 100);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(command);
        (await Validate().ValidateAsync(command)).Errors.Single().ErrorMessage.ShouldEqual("Monthly limit reached");
        _unit.HasEnrolledDecisionReads.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    [InlineData("On")]
    public void should_validate_blank_template_parts_before_reading(string mode)
    {
        var command = CommandFor(mode, " ");
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
        var failure = Catch.Exception(() => Resolve());
        failure.ShouldBeOfExactType<ReadModelStreamPartMissing>();
        ((IValidationFailure)failure).ValidationResult.Message.ShouldEqual("'Month' is required to read MonthlyWithdrawals");
        ((IValidationFailure)failure).ValidationResult.Members.ShouldEqual(["month"]);
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Off", null, null)]
    [InlineData("Measure", null, null)]
    [InlineData("On", null, null)]
    [InlineData("Off", "Limits", "BankAccount")]
    [InlineData("Measure", "Limits", "BankAccount")]
    [InlineData("On", "Limits", "BankAccount")]
    [InlineData("Off", "Transactions", "Other")]
    [InlineData("Measure", "Transactions", "Other")]
    [InlineData("On", "Transactions", "Other")]
    public void should_refuse_a_route_mismatch_in_every_mode(string mode, string? stream, string? source)
    {
        var command = CommandFor(mode, "2026-10");
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command, stream, source);
        Catch.Exception(() => Resolve()).ShouldBeOfExactType<ReadModelStreamNotOnCommandRoute>();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Off", false)]
    [InlineData("Measure", false)]
    [InlineData("On", false)]
    [InlineData("Off", true)]
    [InlineData("Measure", true)]
    [InlineData("On", true)]
    public void should_refuse_a_default_stream_id_in_every_mode(string mode, bool template)
    {
        var command = CommandFor(mode, template ? EventStreamId.Default : "2026-10");
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
        if (!template) _context.Values[WellKnownCommandContextKeys.EventStreamId] = (EventStreamId)EventStreamId.Default;
        Catch.Exception(() => Resolve()).ShouldBeOfExactType<ReadModelStreamNotOnCommandRoute>();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_an_undeclared_stream_id()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(NoStreamId));
        Begin(new NoStreamId(_account));
        Catch.Exception(() => Resolve()).ShouldBeOfExactType<ReadModelStreamNotOnCommandRoute>();
    }

    [Fact]
    public void should_report_an_unspecified_source_as_invalid_input()
    {
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = EventSourceId.Unspecified;
        var failure = Catch.Exception(() => Resolve());
        failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
        ((IValidationFailure)failure).ValidationResult.Message.ShouldEqual("MonthlyWithdrawals '' does not exist.");
        ((IValidationFailure)failure).ValidationResult.Reason.ShouldEqual(ValidationResultReason.Rule);
    }

    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    [InlineData("On")]
    public void should_refuse_lookup_redirection(string mode)
    {
        var command = CommandFor(mode, "2026-10");
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
        var failure = Catch.Exception(() => Resolve(nameof(Withdraw.Redirect)));
        failure.ShouldBeOfExactType<InvalidLookUpBy>();
        failure.Message.ShouldContain("the stream belongs to the command's own event source");
    }

    [Theory]
    [InlineData("Off")]
    [InlineData("Measure")]
    public async Task should_read_the_same_narrowed_value_without_a_guard(string mode)
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        var command = CommandFor(mode, "2026-10");
        using var policy = DecisionPolicyForSpecs.Begin(command.GetType());
        Begin(command);
        ((MonthlyWithdrawals)Resolve()!).Total.ShouldEqual(500);
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
        await Compete("2026-10");
        (await Complete()).IsSuccess.ShouldBeTrue();
        _unit.GetAppendConditions().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_allow_an_advisory_stream_model_without_enrollment()
    {
        await Append("2026-10", 500);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        ((MonthlyWithdrawals)Resolve(nameof(Withdraw.Advisory))!).Total.ShouldEqual(500);
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public async Task should_widen_when_a_whole_source_model_is_also_read()
    {
        await Append("2026-10", 500);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        Resolve(nameof(Withdraw.Mix), 0).ShouldNotBeNull();
        Resolve(nameof(Withdraw.Mix), 1).ShouldNotBeNull();
        await Compete("2026-11");
        var result = await Complete();
        result.IsSuccess.ShouldBeFalse();
        result.ValidationResults.ShouldNotBeEmpty();
        result.ValidationResults.All(failure => failure.ReasonDetail == "ReadChanged").ShouldBeTrue();
    }

    [Fact]
    public async Task should_separate_explicit_streams_in_the_invocation_cache()
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100));
        var reads = _services.GetRequiredService<IDecisionReads>();
        var october = await reads.Get<MonthlyWithdrawals>(_account, "Transactions", "2026-10", "BankAccount");
        var november = await reads.Get<MonthlyWithdrawals>(_account, "Transactions", "2026-11", "BankAccount");
        october.Instance!.Total.ShouldEqual(500);
        november.Instance!.Total.ShouldEqual(8000);
        (await reads.Get<MonthlyWithdrawals>(_account, "Transactions", "2026-10", "BankAccount")).ShouldBeSame(october);
        var failure = await Catch.Exception(async () => await reads.Get<MonthlyWithdrawals>((ReadModelKey)_account.Value));
        failure.ShouldBeOfExactType<DecisionReadRefused>();
        ((DecisionReadRefused)failure!).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
        new ReadProtectionCommandExceptionMapper().TryMap(_context, failure, out var result).ShouldBeTrue();
        result!.ExceptionMessages.Single().ShouldContain("take it as a parameter of a command routed to that stream");
    }

    [Fact]
    public async Task should_support_the_stream_query_overload_and_refuse_key_only_queries()
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        (await MonthlyWithdrawals.ForMonth(_account, "2026-10", _store.ReadModels))!.Total.ShouldEqual(500);
        var failure = await Catch.Exception(async () => await _store.ReadModels.GetInstanceById<MonthlyWithdrawals>((ReadModelKey)_account.Value));
        failure.ShouldBeOfExactType<DecisionReadRefused>();
        ((DecisionReadRefused)failure).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    }

    [Fact]
    public async Task should_route_an_explicit_decision_parameter_without_on()
    {
        await Append("2026-10", 500);
        await Append("2026-11", 8000);
        using var policy = DecisionPolicyForSpecs.Begin(typeof(ExplicitDecision));
        Begin(new ExplicitDecision(_account, "2026-10"));
        _services.GetRequiredService<DecisionRead<MonthlyWithdrawals>>().Instance!.Total.ShouldEqual(500);
        await Compete("2026-10");
        var result = await Complete();
        result.IsSuccess.ShouldBeFalse();
        result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    }

    [Fact]
    public async Task should_measure_widening_from_the_actual_bound_stream_and_whole_source_reads()
    {
        await Append("2026-10", 500);
        var coarsened = false;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, current) =>
            {
                if (instrument.Meter.Name == "Cratis.Arc.ReadProtection") current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var own = false;
            var category = false;
            var reason = false;
            foreach (var tag in tags)
            {
                if (tag.Key == "command" && (tag.Value as string) == typeof(Measure).FullName) own = true;
                if (tag.Key == "category" && (tag.Value as string) == "coarsened") category = true;
                if (tag.Key == "reason" && (tag.Value as string) == "stream_dims_dropped") reason = true;
            }
            if (own && category && reason) coarsened = true;
        });
        listener.Start();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Measure));
        Begin(new Measure(_account, "2026-10", 100));
        var measurement = ReadProtectionMeasurement.Begin(_context)!;
        try
        {
            await measurement.CaptureBoundary();
            ((MonthlyWithdrawals)Resolve()!).Total.ShouldEqual(500);

            // The legacy testing reader has no pinned whole-source instance; absence is still a read.
            ReadModelServiceCollectionExtensions.ResolveReadModel(typeof(AccountWithdrawals), _context, _store.ReadModels).ShouldBeNull();
            coarsened.ShouldBeTrue();
            _unit.HasEnrolledDecisionReads.ShouldBeFalse();
            (await Complete()).IsSuccess.ShouldBeTrue();
            _unit.GetAppendConditions().ShouldBeEmpty();
        }
        finally
        {
            measurement.End();
        }
    }

    object CommandFor(string mode, string month) => mode switch
    {
        "Off" => new Off(_account, month, 100),
        "Measure" => new Measure(_account, month, 100),
        _ => new Withdraw(_account, month, 100)
    };
}

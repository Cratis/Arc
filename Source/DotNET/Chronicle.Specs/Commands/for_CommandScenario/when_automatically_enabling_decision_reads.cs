// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_decision_mode;
using static Cratis.Arc.Chronicle.ReadModels.for_per_stream_models.given.a_monthly_account;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_automatically_enabling_decision_reads
{
    [Fact]
    public async Task should_enable_on_before_seeding_and_report_read_changed()
    {
        await using var scenario = new CommandScenario<AutomaticRead>();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(id, new DecisionStateChanged());
        var result = await scenario.Execute(new(id));
        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionMessages));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        scenario.AppendedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_protect_a_validator_only_read()
    {
        await using var scenario = new CommandScenario<ValidatorOnly>();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(id, new DecisionStateChanged());
        var result = await scenario.Execute(new(id));
        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionMessages));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        scenario.AppendedEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_fold_a_per_stream_model_without_an_opt_in()
    {
        await using var scenario = new CommandScenario<OffStreamRead>();
        var id = new AccountId("automatic-stream");
        var route = scenario.EventRoutes.For<BankAccount>("Transactions").WithStreamId("2026-10");
        scenario.Given.ForEventSource(id).OnRoute(route).Events(new MoneyWithdrawn(500));
        (await scenario.Execute(new(id, "2026-10"))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<OffStreamRead, MoneyWithdrawn>(id, fact => fact.Amount == 500);
    }

    [Fact]
    public async Task should_enable_measure_without_adding_a_guard()
    {
        await using var scenario = new CommandScenario<AutomaticMeasure>();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(id, new DecisionStateChanged());
        (await scenario.Execute(new(id))).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<AutomaticMeasure, DecisionFinished>(id, fact => fact.WasPresent);
    }

    [Fact]
    public async Task should_keep_off_commands_without_protected_dependencies_in_legacy_mode()
    {
        await using var scenario = new CommandScenario<Ordinary>();
        var id = EventSourceId.New();
        scenario.EventScenario.ShouldNotBeNull();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        (await scenario.EventLog.HasEventsFor(id)).ShouldBeFalse();
        (await scenario.Execute(new(id))).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_keep_the_explicit_compatibility_method_working()
    {
        await using var scenario = new CommandScenario<Ordinary>().UseDecisionReads();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        (await scenario.EventLog.HasEventsFor(id)).ShouldBeTrue();
        (await scenario.Execute(new(id))).ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_apply_the_runtime_cap_without_disabling_the_shared_log()
    {
        await using var scenario = new CommandScenario<AutomaticRead>();
        scenario.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = "Off" }).Build());
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(id, new DecisionStateChanged());
        (await scenario.Execute(new(id))).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(1);
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record AutomaticRead(EventSourceId Id)
    {
        public DecisionFinished Handle(DecisionState? state) => new(state is not null);
    }

    [Command, ReadProtection(ReadProtectionMode.Measure)]
    public record AutomaticMeasure(EventSourceId Id) : AutomaticRead(Id);

    [Command, ReadProtection(ReadProtectionMode.Off), EventSource<BankAccount>("Transactions"), EventStreamId("{Month}")]
    public record OffStreamRead(AccountId Account, string Month)
    {
        public MoneyWithdrawn Handle(MonthlyWithdrawals? month) => new(month?.Total ?? 0);
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record ValidatorOnly(EventSourceId Id)
    {
        public DecisionFinished Handle() => new(true);
    }

    public class ValidatorOnlyValidator : CommandValidator<ValidatorOnly>
    {
        public ValidatorOnlyValidator(DecisionState? state) =>
            RuleFor(command => command.Id).Must(_ => state is not null);
    }

    [Command, ReadProtection(ReadProtectionMode.Off)]
    public record Ordinary(EventSourceId EventSourceId)
    {
        public DecisionFinished Handle() => new(true);
    }
}

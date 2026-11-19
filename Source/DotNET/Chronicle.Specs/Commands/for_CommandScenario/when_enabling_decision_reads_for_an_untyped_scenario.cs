// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_automatically_enabling_decision_reads;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_decision_mode;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_enabling_decision_reads_for_an_untyped_scenario
{
    [Fact]
    public async Task should_fold_seeded_history_for_different_commands()
    {
        await using var scenario = new CommandScenario<object>().EnableDecisionReads();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        (await scenario.Execute(new Ordinary(id), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        (await scenario.Execute(new AutomaticRead(id), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        scenario.AppendedEvents.Count.ShouldEqual(2);
        await scenario.ShouldHaveAppendedEvent<object, DecisionFinished>(id, fact => fact.WasPresent);
    }

    [Fact]
    public async Task should_reject_a_protected_decision_if_its_history_changes()
    {
        await using var scenario = new CommandScenario<object>().EnableDecisionReads();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).Events(new DecisionStateChanged());
        scenario.AppendConcurrently(id, new DecisionStateChanged());
        var result = await scenario.Execute(new AutomaticRead(id), global::Xunit.TestContext.Current.CancellationToken);
        Assert.False(result.HasExceptions, string.Join(Environment.NewLine, result.ExceptionMessages));
        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
        scenario.AppendedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_to_discard_seeded_state()
    {
        using var scenario = new CommandScenario<object>();
        scenario.Given.ForEventSource(EventSourceId.New()).Events(new DecisionStateChanged());
        Assert.Throws<DecisionReadsMustBeEnabledBeforeSeeding>(scenario.EnableDecisionReads);
    }

    [Fact]
    public async Task should_refuse_to_switch_an_initialized_scenario()
    {
        await using var scenario = new CommandScenario<object>();
        (await scenario.Execute(new Ordinary(EventSourceId.New()), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        Assert.Throws<DecisionReadsMustBeEnabledBeforeExecution>(scenario.EnableDecisionReads);
    }
}

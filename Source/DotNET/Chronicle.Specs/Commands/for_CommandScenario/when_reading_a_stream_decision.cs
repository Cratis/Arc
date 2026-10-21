// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_reading_a_stream_decision
{
    [Fact]
    public async Task should_fold_only_the_selected_stream()
    {
        await using var scenario = new CommandScenario<DecideWithinPeriod>();
        var id = EventSourceId.New();
        scenario.Given.ForEventSource(id).OnRoute(new EventRoute("reports", "approval", "period")).Events(new PeriodValueChanged(10));
        scenario.Given.ForEventSource(id).OnRoute(new EventRoute("reports", "approval", "other")).Events(new PeriodValueChanged(20));
        var result = await scenario.Execute(new(id, "period"));
        result.ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<DecideWithinPeriod, PeriodValueDecided>(id, fact => fact.Value == 10);
    }

    [Theory]
    [InlineData("period", false)]
    [InlineData("other", true)]
    public async Task should_guard_only_the_selected_stream(string competingPeriod, bool succeeds)
    {
        await using var scenario = new CommandScenario<DecideWithinPeriod>();
        var id = EventSourceId.New();
        scenario.AppendConcurrently(id, new EventRoute("reports", "approval", competingPeriod), new PeriodValueChanged(30));
        var result = await scenario.Execute(new(id, "period"));
        result.IsSuccess.ShouldEqual(succeeds);
        if (!succeeds)
        {
            result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
            scenario.AppendedEvents.ShouldBeEmpty();
        }
    }

    [Command]
    [ProtectedDecision]
    [EventSourceType("reports")]
    [EventStreamType("approval")]
    [EventStreamId("{Period}")]
    public record DecideWithinPeriod(EventSourceId Id, string Period)
    {
        public Task<DecisionRead<PeriodState>> Provide(IDecisionReads reads) =>
            reads.Get<PeriodState>(Id, "approval", Period, "reports");

        public PeriodValueDecided Handle(DecisionRead<PeriodState> state) => new(state.Instance?.Value ?? 0);
    }

    [FromEvent<PeriodValueChanged>]
    public record PeriodState([property: Key] string Id, int Value);

    [EventType]
    public record PeriodValueChanged(int Value);

    [EventType]
    public record PeriodValueDecided(int Value);
}

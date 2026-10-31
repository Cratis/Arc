// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_another_key_was_pinned : given.a_pinned_state
{
    readonly EventSourceId _other = EventSourceId.New();

    async Task Because() => _result = await _scenario.Execute(new(_other), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_refuse_a_missing_required_model() => Assert.Contains("does not exist", _result.ValidationResults.Single().Message);
    [Fact] void should_not_commit() => _scenario.AppendedEvents.ShouldBeEmpty();

    [Fact]
    async Task should_supply_null_for_a_nullable_parameter()
    {
        await using var scenario = new CommandScenario<NullableDecision>();
        scenario.Given.ForEventSource(_source).ReadModel(_state);
        (await scenario.Execute(new(_other), global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSuccessful();
        await scenario.ShouldHaveAppendedEvent<NullableDecision, Decided>(_other, fact => fact.Status == "absent");
    }

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record NullableDecision(EventSourceId Id)
    {
        public Decided Handle(State? state) => new(state?.Status ?? "absent");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_the_command_requires_a_decision_token : Specification
{
    CommandScenario<DecideWithToken> _scenario;
    Exception _failure;

    void Establish() => _scenario = new();

    void Because() => _failure = Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New())
        .ReadModel(new given.a_pinned_state.State(Guid.NewGuid(), "pinned")))!;

    [Fact] void should_refuse_the_pin() => _failure.ShouldBeOfExactType<PinnedReadModelCannotProvideDecisionToken>();
    [Fact] void should_name_the_command() => Assert.Contains(nameof(DecideWithToken), _failure.Message);
    [Fact] void should_prescribe_events() => Assert.Contains("plain Handle parameter under [ReadProtection(On)]", _failure.Message);

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ProtectedDecision]
    public record DecideWithToken(EventSourceId Id)
    {
        public given.a_pinned_state.Decided Handle(DecisionRead<given.a_pinned_state.State> state) => new(state.Instance?.Status ?? "absent");
    }
}

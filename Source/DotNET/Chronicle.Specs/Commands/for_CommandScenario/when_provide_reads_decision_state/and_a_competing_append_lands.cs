// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model.given.a_pinned_state;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_provide_reads_decision_state;

public class and_a_competing_append_lands : Specification
{
    readonly EventSourceId _source = EventSourceId.New();
    readonly EventSourceId _target = EventSourceId.New();
    CommandScenario<DecideWithProvidedState> _scenario;
    CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSource(_target).Events(new StateChanged("provided"));
        _scenario.AppendConcurrently(_target, new StateChanged("competing"));
    }

    async Task Because() => _result = await _scenario.Execute(new(_source, _target), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_not_fail_with_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_refuse_the_changed_read() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_report_read_changed() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
    [Fact] void should_not_commit() => _scenario.AppendedEvents.ShouldBeEmpty();

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record DecideWithProvidedState(EventSourceId Id, EventSourceId Target)
    {
        public async Task<State> Provide(IDecisionReads reads) => (await reads.Get<State>((ReadModelKey)Target.Value)).Instance!;
        public Decided Handle(State state) => new(state.Status);
    }
}

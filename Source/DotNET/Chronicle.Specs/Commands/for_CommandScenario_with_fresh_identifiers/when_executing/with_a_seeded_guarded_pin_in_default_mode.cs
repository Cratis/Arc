// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_seeded_guarded_pin_in_default_mode : given.a_default_mode_scenario
{
    void Establish() => _scenario.Given.ForEventSource(_id).Events(new Created("existing"));
    async Task Because() => _error = await Catch.Exception(() => _scenario.Execute(_command));

    [Fact] void should_refuse_the_scenario() => _error.ShouldBeOfExactType<GeneratedIdentifierCollisionRequiresDecisionReads>();
    [Fact] void should_explain_the_missing_proof_and_fix() => _error!.Message.ShouldEqual("The scenario cannot prove a generated-identifier collision without UseDecisionReads(). Create the scenario with new CommandScenario<TCommand>().UseDecisionReads() before seeding events and pinning the generated value.");
    [Fact] void should_not_handle_the_command() => _command.Calls.ShouldEqual(0);
    [Fact] void should_append_nothing() => _scenario.AppendedEvents.ShouldBeEmpty();
}

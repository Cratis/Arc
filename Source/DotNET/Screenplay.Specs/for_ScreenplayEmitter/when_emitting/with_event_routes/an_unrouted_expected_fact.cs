// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class an_unrouted_expected_fact : a_routed_model
{
    void Establish() => _command = _command with { Authoring = _command.Authoring! with { Route = null } };

    void Because() => _result = _emitter.Emit(Model([new("Registers", [], new("Register", SpecificationStateKind.Command, [new("Id", new LiteralSource("account")), new("Month", new LiteralSource("October")), new("Name", new LiteralSource("New"))]), [new("Registered", SpecificationStateKind.Event, [new("Name", new LiteralSource("New"))]) { Route = SpecificationEventRouteModel.NoStream }], [])]), _options);

    [Fact] void should_assert_the_fact_has_no_stream() => _result.Source.ShouldContain("no stream");
    [Fact] void should_not_invent_a_source() => _result.Source.ShouldNotContain("eventsource");
    [Fact] void should_bind_and_round_trip() => AssertBinds();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class a_composite_stream_id : a_routed_model
{
    void Establish() => _command = _command with
    {
        Authoring = _command.Authoring! with
        {
            Route = new("Account", "Transactions", Text, null, null)
            {
                StreamIdParts = [new("Month", Text, new PropertyPathSource("Month")), new("Region", Text, new LiteralSource("east"))]
            }
        }
    };

    void Because() => _result = _emitter.Emit(Model([new("Registers", [Fact("Prior")], new("Register", SpecificationStateKind.Command, [new("Id", new LiteralSource("account")), new("Month", new LiteralSource("October")), new("Name", new LiteralSource("New"))]), [Fact("New")], [])]), _options);

    [Fact] void should_declare_both_parts() => _result.Source.ShouldContain("month String");
    [Fact] void should_map_a_direct_input() => _result.Source.ShouldContain("month = month");
    [Fact] void should_map_a_literal() => _result.Source.ShouldContain("region = \"east\"");
    [Fact] void should_state_given_and_then_composite_routes() => _result.Source.Split("month = \"October\"", StringSplitOptions.None).Length.ShouldEqual(4);
    [Fact] void should_report_no_omissions() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_and_round_trip() => AssertBinds();

    static SpecificationStateModel Fact(string name) => new("Registered", SpecificationStateKind.Event, [new("Name", new LiteralSource(name))])
    {
        For = new("account"),
        Route = new("Account", "Transactions", null)
        {
            StreamIdParts = [new("Month", new LiteralSource("October")), new("Region", new LiteralSource("east"))]
        }
    };
}

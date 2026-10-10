// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class an_unadmitted_mapping : a_routed_model
{
    [Theory]
    [InlineData("path", "property-path", "PLAY0268")]
    [InlineData("generated", "generated-property", "PLAY0273")]
    [InlineData("handler", "handler commands", "PLAY0268")]
    [InlineData("partial", "at least two distinct parts", "PLAY0273")]
    void should_leave_out_the_entire_route_with_a_typed_reason(string shape, string reason, string playCode)
    {
        var route = _command.Authoring!.Route!;
        _command = shape switch
        {
            "path" => _command with { Authoring = _command.Authoring with { Route = route with { StreamId = "Address.Month" } } },
            "generated" => _command with { Authoring = _command.Authoring with { Generated = [new("Month", Text)] } },
            "handler" => _command with { Produces = [] },
            _ => _command with { Authoring = _command.Authoring with { Route = route with { StreamIdType = null, StreamId = null, StreamIdParts = [new("Month", Text, new PropertyPathSource("Month"))] } } }
        };
        _result = _emitter.Emit(Model(), _options);

        _result.Source.ShouldNotContain("eventsource Account");
        _result.Source.ShouldNotContain("stream Account.Transactions");
        var diagnostic = _result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute);
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        diagnostic.Message.ShouldContain(reason);
        diagnostic.Message.ShouldContain(playCode);
    }
}

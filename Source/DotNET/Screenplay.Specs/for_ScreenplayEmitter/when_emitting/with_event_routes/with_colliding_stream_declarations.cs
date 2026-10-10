// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayEmitter.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting.with_event_routes;

public class with_colliding_stream_declarations : a_routed_model
{
    void Because()
    {
        var other = _command with { Name = "Rename", Authoring = _command.Authoring! with { Route = _command.Authoring!.Route! with { StreamIdType = new("Uuid", false, false) } } };
        var model = Model();
        _result = _emitter.Emit(model with { Slices = model.Slices.Select(slice => slice with { Commands = [_command, other] }).ToList() }, _options);
    }

    [Fact] void should_withhold_both_colliding_routes() => _result.Source.ShouldNotContain("eventsource Account");
    [Fact] void should_report_both_commands() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(2);
    [Fact] void should_report_a_typed_stored_name_collision() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).All(diagnostic => diagnostic.Message.Contains("PLAY0273", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_bind_the_commands() => AssertBinds();
}

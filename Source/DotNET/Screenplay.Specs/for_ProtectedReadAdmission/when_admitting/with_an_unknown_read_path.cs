// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_an_unknown_read_path : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0784"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_withhold_the_named_read_not_the_read_in_the_message() => _admitted.Emission.Source.ShouldContain("reads RoomOccupancy as alternative");
    [Fact] void should_leave_the_other_commands_read() => _admitted.Emission.Source.Split("reads RoomOccupancy as occupancy", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_report_a_generator_defect_at_the_command() => _admitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Location.ShouldEqual("Library.Rooms.Booking.BookRoom");
    [Fact] void should_preserve_the_path_diagnostic() => _admitted.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Message.ShouldContain("PLAY0784: Wording has changed;");
}

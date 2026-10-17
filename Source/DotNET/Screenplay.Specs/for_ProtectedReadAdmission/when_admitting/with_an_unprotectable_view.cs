// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_an_unprotectable_view : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0781"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_withhold_only_the_named_read_of_the_named_command() => AssertOnlyOccupancyWithheld();
    [Fact] void should_locate_the_report_in_csharp() => Reports.Single().Location.ShouldEqual("Library.Rooms.Booking.BookRoom");
    [Fact] void should_preserve_the_reason() => Reports.Single().Message.ShouldContain("PLAY0781: Read 'RoomOccupancy' cannot be protected: NoRemoval.");
}

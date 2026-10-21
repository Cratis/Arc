// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_an_ambiguous_read_path : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0785", "RoomOccupancy"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_withhold_both_reads_of_the_repeated_view() => _admitted.Emission.Source.ShouldNotContain("reads RoomOccupancy as alternative");
    [Fact] void should_leave_the_other_commands_read() => _admitted.Emission.Source.ShouldContain("reads RoomOccupancy as occupancy");
    [Fact] void should_report_a_defect_for_each_read() => _admitted.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind && diagnostic.Message.Contains("PLAY0785", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_not_mistake_the_defect_for_an_admission_limit() => Reports.ShouldBeEmpty();
}

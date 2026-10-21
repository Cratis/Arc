// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_two_refused_reads : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0781"), Refusal("PLAY0781", "alternative"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_report_both_refused_reads_even_with_identical_compiler_messages() => Reports.Count().ShouldEqual(2);
    [Fact] void should_report_the_first_read() => Reports.Count(diagnostic => diagnostic.Message.Contains("Protected read 'occupancy'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_report_the_second_read() => Reports.Count(diagnostic => diagnostic.Message.Contains("Protected read 'alternative'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_withhold_both_reads() => _admitted.Emission.Source.ShouldNotContain("reads RoomOccupancy as alternative");
    [Fact] void should_preserve_the_other_commands_read() => _admitted.Emission.Source.ShouldContain("reads RoomOccupancy as occupancy");
}

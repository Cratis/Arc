// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_command_concurrency : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0783"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_withhold_all_reads_of_the_named_command() => _admitted.Emission.Source.ShouldNotContain("reads RoomOccupancy as alternative");
    [Fact] void should_keep_the_other_commands_read() => _admitted.Emission.Source.ShouldContain("reads RoomOccupancy as occupancy");
    [Fact] void should_report_each_withheld_read() => Reports.Count().ShouldEqual(2);
    [Fact] void should_report_the_compiler_refusal() => Reports.All(diagnostic => diagnostic.Message.Contains("PLAY0783: Wording has changed;", StringComparison.Ordinal)).ShouldBeTrue();
}

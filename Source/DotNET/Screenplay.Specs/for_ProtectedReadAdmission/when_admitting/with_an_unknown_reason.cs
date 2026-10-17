// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_an_unknown_reason : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0781", reason: "future-reason"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_still_withhold_the_read() => AssertOnlyOccupancyWithheld();
    [Fact] void should_report_the_raw_reason() => Reports.Single().Message.ShouldContain("cannot be protected: future-reason.");
}

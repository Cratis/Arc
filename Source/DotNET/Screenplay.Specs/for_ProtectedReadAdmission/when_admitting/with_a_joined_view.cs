// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ProtectedReadAdmission.when_admitting;

public class with_a_joined_view : given.a_refused_document
{
    void Establish() => Refuse(Refusal("PLAY0781", reason: "join"));
    void Because() => _admitted = _admission.Admit(_model, _options, _emission);

    [Fact] void should_withhold_only_the_named_read() => AssertOnlyOccupancyWithheld();
    [Fact] void should_preserve_the_reason() => Reports.Single().Message.ShouldContain("cannot be protected: Join.");
}

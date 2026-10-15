// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ReactorSyntaxBuilder.when_building;

public class with_a_batch_gap_and_code_handling : Specification
{
    ScreenplayDiagnostics _diagnostics;

    void Establish() => _diagnostics = new();

    void Because() => new ReactorSyntaxBuilder(new ScreenplayNaming(), _diagnostics) { Application = ApplicationModel.Empty }.Build(
        new ReactorModel("Welcomer", ["AuthorRegistered"], true, null)
        {
            Reactions = [new("AuthorRegistered", [], []) { UnprovenBatch = new(ReactionBatchGapKind.Closing, "closes before another event") }],
            RefusalHandlingHandlers = ["AuthorRegistered"],
            RedeliveryHandlers = ["AuthorRegistered"]
        },
        "Library.Authors.Welcoming");

    [Fact] void should_report_the_batch_gap() => _diagnostics.All.ShouldContain(report => report.Code == "SP0070");
    [Fact] void should_preserve_the_code_refusal_diagnostic() => _diagnostics.All.ShouldContain(report => report.Code == "SP0071");
    [Fact] void should_preserve_the_code_redelivery_diagnostic() => _diagnostics.All.ShouldContain(report => report.Code == "SP0072");
}

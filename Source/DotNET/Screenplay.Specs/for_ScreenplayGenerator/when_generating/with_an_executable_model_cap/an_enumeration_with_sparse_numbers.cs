// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class an_enumeration_with_sparse_numbers : a_generated_document
{
    const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.Orders.Placing;
        public enum Status { Draft = 1, Issued = 4 }
        [EventType] public record OrderPlaced(Status Status);
        """;

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V11 }, (Analyzed.SlicePath, Source));

    [Fact] void should_keep_bare_names() => Result.Source.ShouldContain("  draft\n  issued\n");
    [Fact] void should_report_the_cap_and_lost_identity() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable).Message.ShouldContain("needs ESM v12, cap v11.0");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_bind_below_v12() => SemanticVersion.V11.IsAtLeast(Bound.Value!.Model.SemanticVersion).ShouldBeTrue();
}

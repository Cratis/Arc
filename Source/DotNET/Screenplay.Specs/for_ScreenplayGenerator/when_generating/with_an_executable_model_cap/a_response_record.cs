// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_an_executable_model_cap;

public class a_response_record : a_generated_document
{
    const string Source = """
        public record LineId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);
        public record RegisterAuthorResponse(LineId LineId, string Name);
        [Command] public record RegisterAuthor(string Name)
        {
            public (RegisterAuthorResponse, AuthorRegistered) Handle()
            {
                var lineId = new LineId(Guid.NewGuid());
                return (new RegisterAuthorResponse(lineId, Name), new(Name));
            }
        }
        """;

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V6 }, (Analyzed.SlicePath, IdentifierSources.With(Source)));

    [Fact] void should_withhold_non_identifier_generated_values() => Result.Source.ShouldNotContain("generated");
    [Fact] void should_withhold_the_response_record() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_keep_the_legacy_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_report_the_cap_with_sp0052() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Message.ShouldContain("ESM v6.0");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_bind_below_v7() => SemanticVersion.V6.IsAtLeast(Bound.Value!.Model.SemanticVersion).ShouldBeTrue();
}

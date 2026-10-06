// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_generated_identifier : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(Name));
            }
        }
        """));

    [Fact] void should_emit_the_generated_identifier() => Result.Source.ShouldContain("authorId AuthorId generated identifier");
    [Fact] void should_return_the_generated_identifier() => Result.Source.ShouldContain("returns authorId");
    [Fact] void should_route_the_event_to_the_returned_identity() => Result.Source.ShouldContain("for authorId");
    [Fact] void should_retire_the_old_diagnostic_for_this_shape() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).ShouldBeEmpty();
    [Fact] void should_compile_and_reject_only_executable_admission() => AssertAuthoringDocument();
    [Fact] void should_not_generate_or_return_an_identity_by_default() => Off.Source.Contains("generated", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_opt_in_when_disabled() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult).Message.ShouldContain("AuthoringOnlyConstructs");
}

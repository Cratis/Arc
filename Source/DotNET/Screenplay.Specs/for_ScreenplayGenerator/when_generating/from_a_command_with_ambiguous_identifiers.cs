// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Several candidates never cause the generator to choose an arbitrary destination.
/// </summary>
public class from_a_command_with_ambiguous_identifiers : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, AuthorId OtherId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_not_mark_an_identifier() => Result.Source.ShouldNotContain(" identifier");
    [Fact] void should_not_state_a_destination() => Result.Source.ShouldNotContain("for id");
    [Fact] void should_report_the_ambiguity_once() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousCommandIdentifier).ShouldEqual(1);
    [Fact] void should_report_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousCommandIdentifier).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

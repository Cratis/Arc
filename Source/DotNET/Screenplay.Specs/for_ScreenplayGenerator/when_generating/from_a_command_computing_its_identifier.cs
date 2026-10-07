// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A computed provider identity must not fall back to a different candidate property.
/// </summary>
public class from_a_command_computing_its_identifier : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, string Name) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() => EventSourceId.New();
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_not_guess_an_identifier() => Result.Source.ShouldNotContain(" identifier");
    [Fact] void should_report_the_unreadable_identity() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandIdentifier).ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

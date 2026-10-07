// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A directly returned property wins over the other key candidates on a self-providing command.
/// </summary>
public class from_a_command_providing_its_identifier : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, AuthorId OtherId, string Name) : ICanProvideEventSourceId
        {
            EventSourceId ICanProvideEventSourceId.GetEventSourceId()
            {
                return this.OtherId;
            }

            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_mark_only_the_provided_identity() => Result.Source.ShouldContain("otherId AuthorId identifier");
    [Fact] void should_not_mark_the_other_candidate() => Result.Source.ShouldNotContain("id AuthorId identifier");
    [Fact] void should_route_to_the_returned_property() => Result.Source.ShouldContain("for otherId");
    [Fact] void should_report_no_ambiguity() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The non-generic Chronicle identity is recognized without an attribute.
/// </summary>
public class from_a_command_with_an_untyped_event_source_id : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(EventSourceId Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_mark_the_identity() => Result.Source.ShouldContain("id EventSourceId identifier");
    [Fact] void should_state_the_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

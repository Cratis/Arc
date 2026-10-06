// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A key declared on a base record remains an input and an identity of the derived command.
/// </summary>
public class from_a_command_inheriting_a_key_property : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public record Identified
        {
            [Key] public Guid Id { get; init; }
        }

        [Command]
        public record RegisterAuthor(string Name) : Identified
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_mark_the_inherited_key() => Result.Source.ShouldContain("id Uuid identifier");
    [Fact] void should_state_the_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

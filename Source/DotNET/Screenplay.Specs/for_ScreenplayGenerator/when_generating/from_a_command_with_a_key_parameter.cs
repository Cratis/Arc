// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Chronicle reads the key from the matching constructor parameter as well as from a property.
/// </summary>
public class from_a_command_with_a_key_parameter : a_batch_a_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor([Key] Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_mark_the_key() => Result.Source.ShouldContain("id Uuid identifier");
    [Fact] void should_state_the_destination() => Result.Source.ShouldContain("for id");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

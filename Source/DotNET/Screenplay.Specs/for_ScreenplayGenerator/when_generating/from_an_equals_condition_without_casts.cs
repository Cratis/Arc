// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_equals_condition_without_casts : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, string Name, int Value)
        {
            public AuthorRegistered? Handle()
            {
                if (object.Equals(Value, 0)) return new(Name);
                return null;
            }
        }
        """)));

    [Fact] void should_keep_the_condition() => Result.Source.ShouldContain("produces when value == 0");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

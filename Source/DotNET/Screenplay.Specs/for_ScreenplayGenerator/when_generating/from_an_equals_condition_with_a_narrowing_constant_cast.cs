// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_equals_condition_with_a_narrowing_constant_cast : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, string Name, int Value)
        {
            public AuthorRegistered? Handle()
            {
                if (object.Equals(Value, (int)unchecked((byte)256))) return new(Name);
                return null;
            }
        }
        """)));

    [Fact] void should_not_recover_a_condition_the_casts_change() => Result.Source.ShouldNotContain("value == 0");
    [Fact] void should_not_state_a_conditional_production() => Result.Source.ShouldNotContain("produces when");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

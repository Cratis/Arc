// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_conditional_single_event_producer : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor(AuthorId Id, string Name, bool Active)
        {
            public AuthorRegistered? Handle()
            {
                if (Active) return new(Name);
                return null;
            }
        }
        """)));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_keep_the_condition() => Result.Source.ShouldContain("produces when active == true");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

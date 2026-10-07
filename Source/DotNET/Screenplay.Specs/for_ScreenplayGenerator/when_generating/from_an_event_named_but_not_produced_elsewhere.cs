// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_named_but_not_produced_elsewhere : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command]
        public record RegisterAuthor([Key] Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        public class EventNames
        {
            public string Name => nameof(AuthorRegistered);
        }
        """)));

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_count_only_the_production() => Result.Model.EventProducerCounts.Values.Single().ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

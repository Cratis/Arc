// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_two_commands_reaching_one_aggregate_creation : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, AggregateRoutingSources.With("""
        public Task Handle(Author author) => author.Register(Name);
        """) + """
        [Command]
        public record RegisterAnotherAuthor([Key] Guid Id, string Name)
        {
            public Task Handle(Author author) => author.Register(Name);
        }
        """));

    [Fact] void should_keep_the_shared_event_standalone() => Result.Source.ShouldContain("event AuthorRegistered");
    [Fact] void should_not_inline_the_shared_event() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_keep_both_productions() => Result.Source.Split("produces AuthorRegistered", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

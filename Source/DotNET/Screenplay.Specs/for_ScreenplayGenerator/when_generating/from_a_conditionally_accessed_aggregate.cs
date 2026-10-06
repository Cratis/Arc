// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_conditionally_accessed_aggregate : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, AggregateRoutingSources.With("""
        public Task Handle(Author author) => author.Register(Name);
        """).Replace("=> Apply(new AuthorRegistered(name));", "=> this?.Apply(new AuthorRegistered(name));", StringComparison.Ordinal)));

    [Fact] void should_retain_command_context() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Single().UsesCommandContext.ShouldBeTrue();
    [Fact] void should_not_inline_a_conditional_production() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldContain("event AuthorRegistered");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

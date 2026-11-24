// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_recovering_graph_property_mappings : given.a_generated_document
{
    void Because() => Generate(given.GraphSources.Files("""
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        using Library.People.Changes;
        namespace Library.Graphs;
        [Graph("People"), Node]
        public record Person(
            [Key, SetFromContext<PersonRegistered>("EventSourceId")] string Id,
            [SetValue<PersonRegistered>("true")] string Text,
            [SetValue<PersonRegistered>(true)] bool Active,
            [Count<PersonRegistered>] int Count,
            [Increment<PersonRegistered>] int Up,
            [Decrement<PersonRegistered>] int Down,
            [AddFrom<PersonRegistered>(nameof(PersonRegistered.Amount))] int Total,
            [SubtractFrom<PersonRegistered>(nameof(PersonRegistered.Amount))] int Remaining);
        """));

    [Fact] void should_compile_and_bind_the_implicit_property_event() => AssertDocument();
    [Fact] void should_preserve_string_literals_without_boolean_inference() => Result.Source.ShouldContain("text = \"true\"");
    [Fact] void should_preserve_boolean_literals() => Result.Source.ShouldContain("active = true");
    [Fact] void should_preserve_count() => Result.Source.ShouldContain("count count");
    [Fact] void should_preserve_increment() => Result.Source.ShouldContain("increment up");
    [Fact] void should_preserve_decrement() => Result.Source.ShouldContain("decrement down");
    [Fact] void should_preserve_addition() => Result.Source.ShouldContain("add total by amount");
    [Fact] void should_preserve_subtraction() => Result.Source.ShouldContain("subtract remaining by amount");
}

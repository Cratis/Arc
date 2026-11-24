// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_recovering_a_declaration_only_graph : given.a_generated_document
{
    void Because() => Generate(("Library/Graphs/People.cs", """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Concepts;
        [assembly: Graph("People")]
        namespace Library.Graphs;
        public record PersonId(string Value) : ConceptAs<string>(Value);
        [Node] public record Person([Key] PersonId Id);
        """));

    [Fact] void should_compile_and_bind() => AssertDocument();
    [Fact] void should_preserve_the_graph_without_an_ordinary_slice() => Bound.Value.Model.Application.Graphs.Single().Name.ShouldEqual("People");
    [Fact] void should_not_invent_handlers() => Bound.Value.Model.Application.GraphProjections.ShouldBeEmpty();
    [Fact] void should_declare_a_graph_only_concept() => Result.Source.ShouldContain("concept PersonId");
    [Fact] void should_keep_the_concept_reference() => Result.Source.ShouldContain("id PersonId key");
}

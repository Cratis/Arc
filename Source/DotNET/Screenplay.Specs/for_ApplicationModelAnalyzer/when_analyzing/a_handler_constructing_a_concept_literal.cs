// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_handler_constructing_a_concept_literal : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Projects.Registration;

        public record ProjectName(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record ProjectRegistered(ProjectName Name);

        [Command]
        public record RegisterProject
        {
            public ProjectRegistered Handle() => new(new ProjectName("Screenplay"));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_not_treat_direct_concept_construction_as_a_global_mapping_literal() => _analysis.Slice().Commands.Single().Produces.Single().Mappings.ShouldBeEmpty();
}

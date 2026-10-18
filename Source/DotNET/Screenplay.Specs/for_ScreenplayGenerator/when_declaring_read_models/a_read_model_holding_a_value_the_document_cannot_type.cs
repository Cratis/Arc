// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding an unnameable dictionary keeps its representable fields, while the projection is withheld
/// rather than claiming to build the original complete shape.
/// </summary>
public class a_read_model_holding_a_value_the_document_cannot_type : a_read_model_document
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Inventory.Tallies;

        [EventType]
        public record TallyStarted(string Name, IDictionary<string, int> Counts);

        [ReadModel]
        public record Tally(string TallyId, string Name, IDictionary<string, int> Counts)
        {
            public static Tally? ById(string tallyId) => null;
        }

        public class Tallies : IProjectionFor<Tally>
        {
            public void Define(IProjectionBuilderFor<Tally> builder) => builder
                .From<TallyStarted>(from => from
                    .Set(model => model.Name).To(e => e.Name)
                    .Set(model => model.Counts).To(e => e.Counts));
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_declare_the_partial_read_model() => Result.Source.ShouldContain("readmodel Tally");
    [Fact] void should_leave_out_the_projection() => Result.Source.ShouldNotContain("projection ");
    [Fact] void should_say_which_property_was_omitted() => Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference && diagnostic.Message.Contains("Tally.Counts", StringComparison.Ordinal));
    [Fact] void should_omit_the_unnameable_property() => Result.Source.ShouldNotContain("counts ");
    [Fact] void should_report_the_withheld_projection() => Result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableTypeReference && diagnostic.Message.Contains("Projection", StringComparison.Ordinal));
    [Fact] void should_bind_the_partial_document() => AssertDocument();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}

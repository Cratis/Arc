// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_recovering_graph_annotations : given.a_generated_document
{
    SemanticApplication Application => Bound.Value.Model.Application;

    void Because() => Generate(given.GraphSources.Files());

    [Fact] void should_compile_and_bind_real_chronicle_annotations() => AssertDocument();
    [Fact] void should_recover_one_graph() => Application.Graphs.Length.ShouldEqual(1);
    [Fact] void should_recover_two_node_labels() => Application.Graphs.Single().Nodes.Length.ShouldEqual(2);
    [Fact] void should_recover_the_edge() => Application.Graphs.Single().Edges.Single().Type.ShouldEqual("WORKS_AT");
    [Fact] void should_recover_edge_identity() => Application.Graphs.Single().Edges.Single().IdentifiedBy.ShouldEqual("employmentId");
    [Fact] void should_exclude_endpoint_storage_properties() => Application.Graphs.Single().Edges.Single().Properties.Any(property => property.Name == "source").ShouldBeFalse();
    [Fact] void should_recover_creation_and_removal_handlers() => Application.GraphProjections.Single().Edges.Single().Handlers.Length.ShouldEqual(2);
    [Fact] void should_recover_detaching_node_removal() => Application.GraphProjections.Single().Nodes.Single().Handlers.Single(handler => handler.Removes).Detach.ShouldBeTrue();

    /// <summary>This is the deterministic Arc fallback, not a claim to recover Stage's original projection name or slice.</summary>
    [Fact] void should_name_the_projection_for_the_event_slice() => Application.GraphProjections.Single().Name.ShouldEqual("Changes");
    [Fact] void should_not_turn_graph_elements_into_read_models() => Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.ReadModels).ShouldBeEmpty();
    [Fact] void should_preserve_key_and_value_mappings() => Result.Source.ShouldContain("salary = salary");
}

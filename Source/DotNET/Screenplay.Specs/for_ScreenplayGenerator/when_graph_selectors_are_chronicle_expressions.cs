// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator;

public class when_graph_selectors_are_chronicle_expressions : given.a_generated_document
{
    SemanticGraphProjection Projection => Bound.Value.Model.Application.GraphProjections.Single();

    void Because() => Generate(given.GraphSources.Files(given.GraphSources.ExpressionGraph));

    [Fact] void should_compile_and_bind_expression_keys() => AssertDocument();
    [Fact] void should_keep_node_creation_and_removal_keys_as_event_source_identity() => Projection.Nodes.Single().Handlers.All(handler => handler.Key?.Kind == SemanticProjectionValueKind.EventSourceIdentity).ShouldBeTrue();
    [Fact] void should_keep_edge_creation_and_removal_sources_as_event_source_identity() => Projection.Edges.Single().Handlers.All(handler => handler.Source?.Kind == SemanticProjectionValueKind.EventSourceIdentity).ShouldBeTrue();
    [Fact] void should_keep_edge_targets_as_event_context() => Projection.Edges.Single().Handlers.All(handler => handler.Target?.Kind == SemanticProjectionValueKind.EventContext).ShouldBeTrue();
    [Fact] void should_canonicalize_event_context_event_source_id_for_identifying_edge_keys() => Projection.Edges.Single().Handlers.All(handler => handler.Identity?.Kind == SemanticProjectionValueKind.EventSourceIdentity).ShouldBeTrue();
}

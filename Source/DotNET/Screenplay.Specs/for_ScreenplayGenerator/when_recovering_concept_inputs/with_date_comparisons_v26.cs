// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_inputs;

public class with_date_comparisons_v26 : a_generated_document
{
    /// <summary>
    /// Screenplay 021a25a3e: DateComparisons/v26/source.declarations.play and source.module.play.
    /// Stage d95756647: SemanticCommandResultRendering and SemanticStateChangeArtifactRenderer shapes.
    /// Only the three raw date inputs become Instant; comparison validators and execution are omitted.
    /// Library replaces the Scheduling module root, retaining the Windows/Keep and Windows/Compare owners.
    /// </summary>
    const string Concepts = """
        using System;
        using Cratis.Concepts;
        namespace Library.Common;
        public record Instant(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value);
        """;

    const string Keep = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Library.Common;
        namespace Library.Windows.Keep;
        [Command]
        public record KeepWindow(string Id, [property: ExplicitOffset] Instant From, [property: ExplicitOffset] Instant To) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() => Id;
            public WindowKept Handle() => new(To);
        }
        [EventType]
        public record WindowKept(Instant To);
        """;

    const string Compare = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Library.Common;
        namespace Library.Windows.Compare;
        [Command]
        public record CompareInstant(string Id, [property: ExplicitOffset] Instant At) : ICanProvideEventSourceId
        {
            public EventSourceId GetEventSourceId() => Id;
            public InstantMatched Handle() => new(At);
        }
        [EventType]
        public record InstantMatched(Instant At);
        """;

    CommandModel _keep;
    CommandModel _compare;
    SemanticSlice _keepSlice;
    SemanticSlice _compareSlice;
    SemanticCommand _boundKeep;
    SemanticCommand _boundCompare;

    void Because()
    {
        Generate(
            ("Library/Program.cs", "namespace Library;"),
            ("Library/Common/Instant.cs", Concepts),
            ("Library/Windows/Keep/Keep.cs", Keep),
            ("Library/Windows/Compare/Compare.cs", Compare));
        _keep = Result.Model.Slices.Single(slice => slice.Name == "Keep").Commands.Single();
        _compare = Result.Model.Slices.Single(slice => slice.Name == "Compare").Commands.Single();
        var slices = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices;
        _keepSlice = slices.Single(slice => slice.Name == "Keep");
        _compareSlice = slices.Single(slice => slice.Name == "Compare");
        _boundKeep = _keepSlice.Commands.Single();
        _boundCompare = _compareSlice.Commands.Single();
    }

    [Fact] void should_recover_the_declared_instant_concept() => Result.Model.Concepts.Single().Name.ShouldEqual("Instant");
    [Fact] void should_recover_the_date_time_backing_type() => Result.Model.Concepts.Single().Primitive.ShouldEqual(ScreenplayPrimitive.DateTime);
    [Fact] void should_keep_from_nominal() => _keep.Properties.Single(property => property.Name == "From").Type.ShouldEqual(new TypeReferenceModel("Instant", false, false));
    [Fact] void should_keep_to_nominal() => _keep.Properties.Single(property => property.Name == "To").Type.ShouldEqual(new TypeReferenceModel("Instant", false, false));
    [Fact] void should_keep_at_nominal() => _compare.Properties.Single(property => property.Name == "At").Type.ShouldEqual(new TypeReferenceModel("Instant", false, false));
    [Fact] void should_recover_the_window_mapping() => _keep.Produces.Single().Mappings.Single().ShouldEqual(new PropertyMappingModel("To", new PropertyPathSource("To")));
    [Fact] void should_recover_the_instant_mapping() => _compare.Produces.Single().Mappings.Single().ShouldEqual(new PropertyMappingModel("At", new PropertyPathSource("At")));
    [Fact] void should_recover_exactly_one_window_production() => _keep.Produces.Count().ShouldEqual(1);
    [Fact] void should_recover_exactly_one_instant_production() => _compare.Produces.Count().ShouldEqual(1);
    [Fact] void should_emit_the_from_concept_input() => Result.Source.ShouldContain("from Instant");
    [Fact] void should_emit_the_to_concept_mapping() => Result.Source.ShouldContain("to Instant = to");
    [Fact] void should_emit_the_at_concept_mapping() => Result.Source.ShouldContain("at Instant = at");
    [Fact] void should_bind_the_concept_backing_type() => Bound.Value.Model.Application.Concepts.Single().Primitive.ShouldEqual(SemanticPrimitiveType.DateTime);
    [Fact] void should_bind_from_to_the_declared_concept() => _boundKeep.Properties.Single(property => property.Name == "from").Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_to_to_the_declared_concept() => _boundKeep.Properties.Single(property => property.Name == "to").Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_at_to_the_declared_concept() => _boundCompare.Properties.Single(property => property.Name == "at").Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_the_window_event_property_as_the_concept() => _keepSlice.Events.Single().Properties.Single().Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_the_instant_event_property_as_the_concept() => _compareSlice.Events.Single().Properties.Single().Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_the_window_event_identity() => _boundKeep.Produces.Single().EventContract.ShouldEqual(_keepSlice.Events.Single().Id);
    [Fact] void should_bind_the_instant_event_identity() => _boundCompare.Produces.Single().EventContract.ShouldEqual(_compareSlice.Events.Single().Id);
    [Fact] void should_bind_the_window_mapping_target() => _boundKeep.Produces.Single().Mappings.Single().TargetProperty.ShouldEqual(_keepSlice.Events.Single().Properties.Single().Id);
    [Fact] void should_bind_the_instant_mapping_target() => _boundCompare.Produces.Single().Mappings.Single().TargetProperty.ShouldEqual(_compareSlice.Events.Single().Properties.Single().Id);
    [Fact] void should_bind_the_window_mapping_source() => _boundKeep.Produces.Single().Mappings.Single().Source.ShouldEqual(new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _boundKeep.Properties.Single(property => property.Name == "to").Id));
    [Fact] void should_bind_the_instant_mapping_source() => _boundCompare.Produces.Single().Mappings.Single().Source.ShouldEqual(new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _boundCompare.Properties.Single(property => property.Name == "at").Id));
    [Fact] void should_report_no_generation_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

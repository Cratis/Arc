// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_inputs;

public class from_declared_date_concepts : a_generated_document
{
    CommandModel _command;
    SemanticSlice _slice;
    SemanticCommand _boundCommand;

    void Because()
    {
        Generate((Analyzed.SlicePath, """
            using System;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Concepts;
            namespace Library.Reservations.Recording;
            public record ReservationDate(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value);
            [EventType]
            public record ReservationRecorded(ReservationDate RecordedAt);
            [Command]
            public record RecordReservation([Key] Guid Id, [property: ExplicitOffset] ReservationDate ReservationDate)
            {
                public ReservationRecorded Handle() => new(RecordedAt: ReservationDate);
            }
            """));
        _command = Result.Model.Slices.Single().Commands.Single();
        _slice = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices.Single();
        _boundCommand = _slice.Commands.Single();
    }

    [Fact] void should_recover_the_declared_concept() => Result.Model.Concepts.Single().Name.ShouldEqual("ReservationDate");
    [Fact] void should_recover_the_date_time_backing_type() => Result.Model.Concepts.Single().Primitive.ShouldEqual(ScreenplayPrimitive.DateTime);
    [Fact] void should_keep_the_command_input_nominal() => _command.Properties.Single(property => property.Name == "ReservationDate").Type.ShouldEqual(new TypeReferenceModel("ReservationDate", false, false));
    [Fact] void should_keep_the_event_property_nominal() => Result.Model.Slices.Single().Events.Single().Properties.Single().Type.ShouldEqual(new TypeReferenceModel("ReservationDate", false, false));
    [Fact] void should_recover_the_explicit_mapping() => _command.Produces.Single().Mappings.Single().ShouldEqual(new PropertyMappingModel("RecordedAt", new PropertyPathSource("ReservationDate")));
    [Fact] void should_emit_the_concept_input() => Result.Source.ShouldContain("reservationDate ReservationDate");
    [Fact] void should_emit_the_differently_named_event_property_mapping() => Result.Source.ShouldContain("recordedAt ReservationDate = reservationDate");
    [Fact] void should_bind_the_concept_backing_type() => Bound.Value.Model.Application.Concepts.Single().Primitive.ShouldEqual(SemanticPrimitiveType.DateTime);
    [Fact] void should_bind_the_input_to_the_declared_concept_identity() => _boundCommand.Properties.Single(property => property.Name == "reservationDate").Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_the_event_property_to_the_same_concept_identity() => _slice.Events.Single().Properties.Single().Type.ShouldEqual(SemanticTypeReference.ForConcept(Bound.Value.Model.Application.Concepts.Single().Id));
    [Fact] void should_bind_the_produced_event_identity() => _boundCommand.Produces.Single().EventContract.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_bind_the_mapping_target_to_the_actual_event_property() => _boundCommand.Produces.Single().Mappings.Single().TargetProperty.ShouldEqual(_slice.Events.Single().Properties.Single().Id);
    [Fact] void should_bind_the_mapping_source_to_the_command_input() => _boundCommand.Produces.Single().Mappings.Single().Source.ShouldEqual(new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _boundCommand.Properties.Single(property => property.Name == "reservationDate").Id));
    [Fact] void should_report_no_generation_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

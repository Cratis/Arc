// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_inputs;

public class from_primitive_date_inputs : a_generated_document
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
            namespace Library.Reservations.Recording;
            [EventType]
            public record ReservationRecorded(DateTimeOffset RecordedAt);
            [Command]
            public record RecordReservation([Key] Guid Id, [property: ExplicitOffset] DateTimeOffset ReservationDate)
            {
                public ReservationRecorded Handle() => new(RecordedAt: ReservationDate);
            }
            """));
        _command = Result.Model.Slices.Single().Commands.Single();
        _slice = Bound.Value.Model.Application.Modules.Single().Features.Single().Slices.Single();
        _boundCommand = _slice.Commands.Single();
    }

    [Fact] void should_not_infer_a_concept_from_the_input_name() => Result.Model.Concepts.ShouldBeEmpty();
    [Fact] void should_keep_the_command_input_primitive() => _command.Properties.Single(property => property.Name == "ReservationDate").Type.ShouldEqual(new TypeReferenceModel("DateTime", false, false));
    [Fact] void should_keep_the_event_property_primitive() => Result.Model.Slices.Single().Events.Single().Properties.Single().Type.ShouldEqual(new TypeReferenceModel("DateTime", false, false));
    [Fact] void should_recover_the_explicit_mapping() => _command.Produces.Single().Mappings.Single().ShouldEqual(new PropertyMappingModel("RecordedAt", new PropertyPathSource("ReservationDate")));
    [Fact] void should_emit_the_primitive_input() => Result.Source.ShouldContain("reservationDate DateTime");
    [Fact] void should_emit_the_differently_named_event_property_mapping() => Result.Source.ShouldContain("recordedAt DateTime = reservationDate");
    [Fact] void should_not_emit_an_invented_concept() => Result.Source.ShouldNotContain("concept ReservationDate");
    [Fact] void should_not_bind_an_invented_concept() => Bound.Value.Model.Application.Concepts.ShouldBeEmpty();
    [Fact] void should_bind_the_input_as_a_primitive() => _boundCommand.Properties.Single(property => property.Name == "reservationDate").Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime));
    [Fact] void should_bind_the_event_property_as_a_primitive() => _slice.Events.Single().Properties.Single().Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime));
    [Fact] void should_bind_the_produced_event_identity() => _boundCommand.Produces.Single().EventContract.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_bind_the_mapping_target_to_the_actual_event_property() => _boundCommand.Produces.Single().Mappings.Single().TargetProperty.ShouldEqual(_slice.Events.Single().Properties.Single().Id);
    [Fact] void should_bind_the_mapping_source_to_the_command_input() => _boundCommand.Produces.Single().Mappings.Single().Source.ShouldEqual(new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, _boundCommand.Properties.Single(property => property.Name == "reservationDate").Id));
    [Fact] void should_report_no_generation_diagnostics() => Result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}

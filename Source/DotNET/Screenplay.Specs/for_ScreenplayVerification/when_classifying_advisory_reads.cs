// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification;

public class when_classifying_advisory_reads : Specification
{
    ScreenplayVerification _legacy;
    ScreenplayVerification _off;
    ScreenplayVerification _measure;
    ScreenplayVerification _capped;
    ScreenplayVerification _missingPreamble;
    ScreenplayVerification _unknown;
    ScreenplayVerification _altered;
    ScreenplayVerification _withoutMetadata;

    void Because()
    {
        _legacy = Verify(Emit());
        _off = Verify(Emit("Off"));
        _measure = Verify(Emit("Measure"));
        _capped = Verify(Emit("On", SemanticVersion.V24));
        var protectedEmission = Emit("On");
        _missingPreamble = Verify(protectedEmission with { Source = protectedEmission.Source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal) });
        var condition = _legacy.BindingDiagnostics.First(diagnostic => diagnostic.Message.StartsWith("Condition operand", StringComparison.Ordinal));
        _unknown = _legacy with { BindingDiagnostics = [Diagnostic.Error(condition.Code, condition.Message.Replace("occupancy.freeBeds", "unknown.freeBeds", StringComparison.Ordinal), condition.Location)] };
        _altered = _legacy with { BindingDiagnostics = [Diagnostic.Error(condition.Code, condition.Message + " changed", condition.Location)] };
        _withoutMetadata = new ScreenplayVerifier().Verify(Emit().Source);
    }

    static ScreenplayVerification Verify(ScreenplayEmission emission) => GeneratedCommandReads.Verify(new ScreenplayVerifier(), emission);

    static ScreenplayEmission Emit(string? protection = null, SemanticVersion? cap = null)
    {
        var attributes = protection is null ? string.Empty : $", ReadProtection(ReadProtectionMode.{protection})";
        var compilation = Analyzed.Compile((Analyzed.SlicePath, $$"""
            using System;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Arc.Validation;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Projections;
            using Cratis.Chronicle.ReadModels;
            using Cratis.Monads;
            namespace Library.Rooms.Booking;
            public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
            [EventType] public record RoomBooked(int Guests, int FreeBeds);
            [ReadModel] public record RoomOccupancy(int FreeBeds)
            {
                public static RoomOccupancy Occupancy() => new(0);
            }
            public class OccupancyProjection : IProjectionFor<RoomOccupancy>
            {
                public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder.From<RoomBooked>(from => from.Set(room => room.FreeBeds).To(e => e.FreeBeds));
            }
            [Command{{attributes}}] public record BookRoom(RoomId RoomId, int Guests)
            {
                public Result<RoomOccupancy, ValidationResult> Provide(DecisionRead<RoomOccupancy> occupancy) =>
                    occupancy.Instance!.FreeBeds >= Guests ? occupancy.Instance : ValidationResult.Error("The room is full");
                public RoomBooked Handle(RoomOccupancy room) => new(Guests, room.FreeBeds);
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var options = new ScreenplayOptions { AuthoringOnlyConstructs = true, MaximumExecutableModelVersion = cap }.WithDefaults(Analyzed.AssemblyName);
        var model = new ApplicationModelAnalyzer().Analyze(compilation, options).Model;
        return new ScreenplayEmitter().Emit(model, options);
    }

    [Fact] void should_exercise_both_real_legacy_read_refusals() => _legacy.BindingDiagnostics.Count(diagnostic => diagnostic.Message.StartsWith("Condition operand", StringComparison.Ordinal) || diagnostic.Message.StartsWith("The produced event mapping expression", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_accept_unannotated_advisory_reads() => _legacy.UnexpectedBindingErrors(true).ShouldBeEmpty();
    [Fact] void should_accept_off_reads() => _off.UnexpectedBindingErrors(true).ShouldBeEmpty();
    [Fact] void should_accept_measure_reads() => _measure.UnexpectedBindingErrors(true).ShouldBeEmpty();
    [Fact] void should_accept_explicit_version_cap_demotions() => _capped.UnexpectedBindingErrors(true).ShouldBeEmpty();
    [Fact] void should_reject_an_on_read_with_a_missing_preamble() => _missingPreamble.UnexpectedBindingErrors(true).ShouldNotBeEmpty();
    [Fact] void should_reject_unknown_aliases() => _unknown.UnexpectedBindingErrors(true).ShouldNotBeEmpty();
    [Fact] void should_reject_altered_messages() => _altered.UnexpectedBindingErrors(true).ShouldNotBeEmpty();
    [Fact] void should_not_exempt_default_mode() => _legacy.UnexpectedBindingErrors(false).ShouldNotBeEmpty();
    [Fact] void should_not_infer_advisory_intent_from_printed_text() => _withoutMetadata.UnexpectedBindingErrors(true).ShouldNotBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_stage_shaped_command : a_protected_document
{
    const string View = """
        [EventType] public record Stored(string Value);
        [EventType] public record Taken(string Value);
        [EventType] public record Discarded();
        [ReadModel]
        public record Current(EventSourceId Id, string Value)
        {
            public static Current? ById(EventSourceId id) => null;
        }
        public class CurrentProjection : IProjectionFor<Current>
        {
            public void Define(IProjectionBuilderFor<Current> builder) => builder
                .From<Stored>(from => from.Set(current => current.Value).To(e => e.Value))
                .From<Taken>(from => from.Set(current => current.Value).To(e => e.Value))
                .RemovedWith<Discarded>();
        }
        """;

    /// <summary>
    /// The shape Stage renders (Stage stage-440-render-c2, when_rendering_protected_reads), spelled out as the renderer writes it.
    /// </summary>
    const string Command = """
        [global::Cratis.Arc.Commands.ReadProtectionAttribute(global::Cratis.Arc.Commands.ReadProtectionMode.On)]
        [Command]
        public record Consume(EventSourceId SourceId)
        {
            public global::OneOf.OneOf<global::Cratis.Chronicle.EventSequences.EventForEventSourceId, global::Cratis.Arc.Validation.ValidationResult> Handle([global::Cratis.Arc.Commands.LookUpByAttribute(nameof(Consume.SourceId))] Current? current)
            {
                if (current is null) return global::OneOf.OneOf<global::Cratis.Chronicle.EventSequences.EventForEventSourceId, global::Cratis.Arc.Validation.ValidationResult>.FromT1(global::Cratis.Arc.Validation.ValidationResult.Error("Current '" + SourceId + "' does not exist."));
                if (!(!global::System.Object.Equals(current.Value, ""))) return global::OneOf.OneOf<global::Cratis.Chronicle.EventSequences.EventForEventSourceId, global::Cratis.Arc.Validation.ValidationResult>.FromT1(global::Cratis.Arc.Validation.ValidationResult.Error("The stored value must not be empty."));
                return global::OneOf.OneOf<global::Cratis.Chronicle.EventSequences.EventForEventSourceId, global::Cratis.Arc.Validation.ValidationResult>.FromT0(new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(SourceId, new Taken(current.Value)));
            }
        }
        """;

    void Because() => Generate(Library(Command, View));

    [Fact] void should_state_the_preamble() => Result.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_name_the_read_by_its_view() => Result.Source.Contains("reads Current by sourceId").ShouldBeTrue();
    [Fact] void should_not_state_an_alias() => Result.Source.Contains("reads Current as").ShouldBeFalse();
    [Fact] void should_state_the_read_backed_requirement() => Result.Source.Contains("require Current.value != \"\"").ShouldBeTrue();
    [Fact] void should_state_the_read_path_mapping() => Result.Source.Contains("value = Current.value").ShouldBeTrue();
    [Fact] void should_report_that_the_absence_message_is_not_carried() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnprotectableCommandRead && diagnostic.Message.Contains("not carried")).ShouldEqual(1);
    [Fact] void should_bind_at_v25() => AssertBindsAtV25(Result);
    [Fact] void should_state_the_same_document_in_authoring_mode() => Authoring.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}

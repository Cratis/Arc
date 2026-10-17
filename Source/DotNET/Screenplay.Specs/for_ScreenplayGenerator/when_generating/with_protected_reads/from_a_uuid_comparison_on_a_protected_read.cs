// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_uuid_comparison_on_a_protected_read : a_protected_document
{
    const string Grant = """
        public record MemberId(Guid Value) : Cratis.Concepts.ConceptAs<Guid>(Value);
        [EventType] public record GrantOpened(MemberId Member);
        [EventType] public record GrantRemoved();
        [ReadModel]
        public record RoomGrant(RoomId Id, MemberId MemberId)
        {
            public static RoomGrant? ById(RoomId id) => null;
        }
        public class GrantProjection : IProjectionFor<RoomGrant>
        {
            public void Define(IProjectionBuilderFor<RoomGrant> builder) => builder
                .From<GrantOpened>(from => from.Set(grant => grant.MemberId).To(e => e.Member))
                .RemovedWith<GrantRemoved>();
        }
        """;

    void Because()
    {
        const string source = """
            [Command, ReadProtection(ReadProtectionMode.On)]
            public record BookRoom(RoomId RoomId, MemberId RequestedBy)
            {
                public Result<RoomGrant, ValidationResult> Provide(RoomGrant grant) =>
                    grant.MemberId != RequestedBy ? ValidationResult.Error("Not your room") : grant;

                public GrantOpened Handle(RoomGrant grant) => new(RequestedBy);
            }
            """;

        Generate(Library(source, Grant), SemanticVersion.V27);
    }

    [Fact] void should_state_the_protected_read() => Result.Source.Contains("reads RoomGrant as grant by roomId").ShouldBeTrue();
    [Fact] void should_state_the_uuid_requirement_over_the_read_path() => Result.Source.Contains("require grant.memberId == requestedBy").ShouldBeTrue();
    [Fact] void should_state_the_protected_preamble() => Result.Source.Contains("command reads protected").ShouldBeTrue();
    [Fact] void should_bind_at_v28() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
    [Fact] void should_have_no_errors() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_bind() => Assert.True(Bound.Success, string.Join(Environment.NewLine, Bound.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    [Fact] void should_withhold_the_comparison_below_v28() => Capped.Source.Contains("require grant.memberId == requestedBy").ShouldBeFalse();
    [Fact] void should_keep_the_protected_read_below_v28() => Capped.Source.Contains("reads RoomGrant as grant by roomId").ShouldBeTrue();
    [Fact] void should_report_the_uuid_version_cap() => Capped.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning && diagnostic.Message.Contains("require ESM v28") && diagnostic.Message.Contains("capped at ESM v27")).ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_uuid_read_document : a_protected_document
{
    const string Grant = """
        [EventType] public record GrantOpened(Guid Owner);
        [EventType] public record GrantRemoved();
        [ReadModel]
        public record RoomGrant(RoomId Id, Guid Owner, Guid? Optional)
        {
            public static RoomGrant? ById(RoomId id) => null;
        }
        public class GrantProjection : IProjectionFor<RoomGrant>
        {
            public void Define(IProjectionBuilderFor<RoomGrant> builder) => builder
                .From<GrantOpened>(from => from.Set(grant => grant.Owner).To(e => e.Owner))
                .RemovedWith<GrantRemoved>();
        }
        """;

    protected void GenerateComparison(string comparison) => Generate(Library($$"""
        [Command, ReadProtection(ReadProtectionMode.On)]
        public record BookRoom(RoomId RoomId, Guid Requester, Guid? Maybe)
        {
            public Result<RoomGrant, ValidationResult> Provide(RoomGrant grant, RoomGrant alternative) =>
                {{comparison}} ? grant : ValidationResult.Error("Not allowed");

            public GrantOpened Handle(RoomGrant grant) => new(Requester);
        }
        """, Grant), SemanticVersion.V28);
}

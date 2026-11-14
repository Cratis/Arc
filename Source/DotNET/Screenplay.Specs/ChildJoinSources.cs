// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Declares a child projection with explicit and automatically mapped joins and a computed read model property.
/// </summary>
public static class ChildJoinSources
{
    /// <summary>
    /// Gets the C# source shared by analysis and generation specifications.
    /// </summary>
    public const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Codes.Listing;

        [EventType]
        public record CodeObserved(Guid CodeId, string Unit);

        [EventType]
        public record CodeUnitObserved(string Unit);

        [EventType]
        public record MonthOpened(string Unit);

        public record Code
        {
            public Guid CodeId { get; init; }
            public string Unit { get; init; } = string.Empty;
            public string ApprovedBy { get; init; } = string.Empty;
            public string StreamId => CodeId.ToString();
        }

        [ReadModel]
        public record Codes
        {
            public Guid Id { get; init; }
            public IEnumerable<Code> Entries { get; init; } = [];
        }

        public class CodesProjection : IProjectionFor<Codes>
        {
            public void Define(IProjectionBuilderFor<Codes> builder) => builder
                .Children(m => m.Entries, c => c
                    .IdentifiedBy(e => e.CodeId)
                    .From<CodeObserved>(_ => _
                        .UsingKey(e => e.CodeId)
                        .Set(m => m.Unit).To(e => e.Unit)
                        .Set(m => m.ApprovedBy).ToEventContextProperty(context => context.CausedBy.Subject))
                    .Join<CodeUnitObserved>(j => j.Set(m => m.Unit).To(e => e.Unit))
                    .Join<MonthOpened>());
        }
        """;
}

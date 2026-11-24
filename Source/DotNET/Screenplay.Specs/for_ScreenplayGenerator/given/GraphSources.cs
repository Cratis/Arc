// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>Real Chronicle annotations in the parameter-targeted shape Stage's graph renderer emits.</summary>
public static class GraphSources
{
    /// <summary>Events declared separately from the graph records.</summary>
    public const string Events = """
        using System;
        using Cratis.Chronicle.Events;
        namespace Library.People.Changes;
        [EventType("PersonRegistered")] public record PersonRegistered(string PersonId, string Name, int Amount);
        [EventType("PersonRemoved")] public record PersonRemoved(string PersonId);
        [EventType("EmploymentStarted")] public record EmploymentStarted(string PersonId, string CompanyId, string EmploymentId, decimal Salary);
        [EventType("EmploymentEnded")] public record EmploymentEnded(string PersonId, string CompanyId, string EmploymentId);
        """;

    /// <summary>A node, its target node and an identifying edge, using Chronicle's actual attributes.</summary>
    public const string Graph = """
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.Projections.Graphs;
        using Cratis.Chronicle.Projections.ModelBound;
        using Library.People.Changes;
        namespace Library.ReadModels.Graphs;
        [Graph("People"), Node, NoAutoMap]
        [FromEvent<PersonRegistered>(nameof(PersonRegistered.PersonId))]
        [RemovedWith<PersonRemoved>(nameof(PersonRemoved.PersonId), Detach = true)]
        public record Person([Key] string Id, [SetFrom<PersonRegistered>(nameof(PersonRegistered.Name))] string Name);
        [Graph("People"), Node, NoAutoMap]
        public record Company([Key] string Id);
        [Graph("People"), Edge("WORKS_AT"), NoAutoMap]
        [EdgeFrom<EmploymentStarted>(nameof(EmploymentStarted.PersonId), nameof(EmploymentStarted.CompanyId), nameof(EmploymentStarted.EmploymentId))]
        [EdgeRemovedWith<EmploymentEnded>(nameof(EmploymentEnded.PersonId), nameof(EmploymentEnded.CompanyId), nameof(EmploymentEnded.EmploymentId))]
        public record WORKS_AT([Source<Person>] string Source, [Target<Company>] string Target,
            [Key] string EmploymentId, [SetFrom<EmploymentStarted>(nameof(EmploymentStarted.Salary))] decimal Salary);
        """;

    /// <summary>Source files, with no stub Chronicle annotations.</summary>
    /// <param name="graph">The graph annotations.</param>
    /// <param name="events">The declaring events.</param>
    /// <returns>The input source files.</returns>
    public static (string Path, string Text)[] Files(string graph = Graph, string events = Events) =>
        [("Library/People/Changes/Events.cs", events), ("Library/ReadModels/Graphs/People.cs", graph)];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class a_protected_document : Specification
{
    protected const string Occupancy = """
        [EventType] public record RoomOpened(int Beds);
        [EventType] public record RoomBooked(int Guests, int FreeBeds);
        [EventType] public record RoomClosed();
        [ReadModel]
        public record RoomOccupancy(RoomId Id, int FreeBeds)
        {
            public static RoomOccupancy? ById(RoomId id) => null;
        }
        public class OccupancyProjection : IProjectionFor<RoomOccupancy>
        {
            public void Define(IProjectionBuilderFor<RoomOccupancy> builder) => builder
                .From<RoomOpened>(from => from.Set(room => room.FreeBeds).To(e => e.Beds))
                .From<RoomBooked>(from => from.Set(room => room.FreeBeds).To(e => e.FreeBeds))
                .RemovedWith<RoomClosed>();
        }
        """;

    protected ScreenplayGenerationResult Result;
    protected ScreenplayGenerationResult Authoring;
    protected ScreenplayGenerationResult Capped;
    protected RoundTripResult RoundTrip;
    protected CompilationResult<SemanticCompilation> Bound;

    protected static string Library(string members, string view = Occupancy, string attributes = "") => $$"""
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Monads;
        {{attributes}}
        namespace Library.Rooms.Booking;
        public record RoomId(Guid Value) : EventSourceId<Guid>(Value);
        {{view}}
        {{members}}
        """;

    protected void Generate(string source, SemanticVersion? cap = null)
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        var generator = new ScreenplayGenerator();
        Result = generator.Generate(compilation, new ScreenplayOptions());
        Authoring = generator.Generate(compilation, new ScreenplayOptions { AuthoringOnlyConstructs = true });
        Capped = generator.Generate(compilation, new ScreenplayOptions { MaximumExecutableModelVersion = cap ?? SemanticVersion.V24 });
        var compiled = new ScreenplayCompiler().Compile(Result.Source);
        RoundTrip = compiled.Value is null ? null : Verification.RoundTrip.For(compiled.Value);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Result.Source);
        Bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
    }

    protected void AssertBindsAtV25(ScreenplayGenerationResult result)
    {
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Assert.True(Bound.Success, string.Join(Environment.NewLine, Bound.Diagnostics.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V25);
    }

    protected static IEnumerable<string> Codes(ScreenplayGenerationResult result) => result.Diagnostics.Select(diagnostic => diagnostic.Code);
}

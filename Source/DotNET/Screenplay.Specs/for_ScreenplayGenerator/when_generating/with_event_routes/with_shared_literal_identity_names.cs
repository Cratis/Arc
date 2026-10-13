// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_shared_literal_identity_names : a_generated_document
{
    [Theory]
    [InlineData("Shared.Notes")]
    [InlineData("nameof(Shared.Notes)")]
    void should_preserve_shared_references_to_declared_literal_identities(string expression)
    {
        var source = IdentifierSources.With($$"""
            public static class Shared
            {
                public const string Notes = "Notes";
            }
            [Cratis.Chronicle.EventSources.EventSource("Account")]
            [Cratis.Chronicle.EventSources.EventStream("Notes")]
            public class Ledger : Cratis.Chronicle.EventSources.IEventSource;
            [Command, Cratis.Arc.Chronicle.Commands.EventSource<Ledger>({{expression}})]
            public record RegisterAuthor(AuthorId AuthorId, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            [Cratis.Chronicle.Reactors.Reactor("Worker")]
            [Cratis.Chronicle.EventSources.FromEventSource<Ledger>({{expression}})]
            public class Worker : Cratis.Chronicle.Reactors.IReactor
            {
                public object AuthorRegistered(AuthorRegistered @event) => new Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Name, new Followed(@event.Name));
            }
            [EventType]
            public record Followed(string Name);
            """);
        Generate((Analyzed.SlicePath, source));
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        var command = compilation.GetTypeByMetadataName("Library.Authors.Registration.RegisterAuthor")!;
        var observer = compilation.GetTypeByMetadataName("Library.Authors.Registration.Worker")!;

        EventSourceReader.Read(command)!.Stream.ShouldEqual("Notes");
        EventSourceReader.ReadObserved(observer)!.Stream.ShouldEqual("Notes");
        Result.Source.ShouldContain("stream Account.Notes");
        Result.Source.ShouldContain("from Account.Notes");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventStreamNotDeclared).ShouldBeEmpty();
        AssertDocument();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A compilation carries more than the project's own files - a referenced package can ship source of its own, and it
/// is compiled from wherever the package cache lives. Letting that decide where the project is puts the file system
/// root in front of every path in the document, so what a reader gets is the absolute layout of the machine that
/// generated it. A document that cannot be committed and diffed is a document nobody regenerates.
/// </summary>
public class source_a_referenced_package_contributed : Specification
{
    const string Slice = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;

        namespace Library.Lending.Notifications;

        [EventType]
        public record BookReserved(string Isbn);

        public class ReservationNotifier : IReactor
        {
            public Task BookReserved(BookReserved @event, EventContext context) => Task.CompletedTask;
        }
        """;

    const string FromAPackage = "global using System;";

    static readonly (string Path, string Text)[] _sources =
    [
        ("/Volumes/work/Library/Lending/Notifications/Notifications.cs", Slice),
        ("/Volumes/work/Library/Program.cs", "namespace Library;"),
        ("/Users/someone/.nuget/packages/cratis/20.8.3/contentFiles/cs/any/GlobalUsings.cs", FromAPackage)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_write_the_path_relative_to_the_project() => _analysis.Slice().Reactors.Single().SourceFilePath.ShouldEqual("Lending/Notifications/Notifications.cs");
}

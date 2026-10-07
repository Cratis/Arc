// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_two_routed_commands : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command] public record RegisterAuthor(string Name)
        {
            public System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog log) =>
                log.Append(EventSourceId.New(), new AuthorRegistered(Name));
        }
        [Command] public record RenameAuthor(string Name)
        {
            public System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog log) =>
                log.Append(EventSourceId.New(), new AuthorRegistered(Name));
        }
        """)));

    [Fact] void should_report_both_destinations() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).Distinct().Count().ShouldEqual(2);
    [Fact] void should_locate_registration() => DiagnosticFor("RegisterAuthor").Location.ShouldEqual("Library.Authors.Registration.RegisterAuthor");
    [Fact] void should_locate_renaming() => DiagnosticFor("RenameAuthor").Location.ShouldEqual("Library.Authors.Registration.RenameAuthor");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();

    ScreenplayDiagnostic DiagnosticFor(string command) => Result.Diagnostics.Single(diagnostic =>
        diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination && diagnostic.Message.Contains($"command '{command}'", StringComparison.Ordinal));
}

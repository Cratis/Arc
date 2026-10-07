// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_two_routed_commands : Specification
{
    EmbeddedDocumentGeneration _generation;

    void Because()
    {
        var slice = given.an_application.Registration();
        var command = slice.Commands.Single();
        var routed = command with { Produces = command.Produces.Select(production => production with { UsesCommandContext = false }).ToList() };
        var model = given.an_application.Build() with
        {
            Slices = [slice with { Commands = [routed, routed with { Name = "RenameAuthor" }] }]
        };
        _generation = new EmbeddedDocumentGenerator().Generate(model, given.an_application.Options(), []);
    }

    [Fact] void should_succeed() => _generation.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_two_distinct_destination_diagnostics() => _generation.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(2);
    [Fact] void should_locate_and_name_registration() => AssertDiagnostic("RegisterAuthor");
    [Fact] void should_locate_and_name_renaming() => AssertDiagnostic("RenameAuthor");

    void AssertDiagnostic(string command)
    {
        var diagnostic = _generation.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination &&
            diagnostic.Location == $"Library.Authors.Registration.{command}");
        diagnostic.Message.ShouldContain($"command '{command}'");
    }
}

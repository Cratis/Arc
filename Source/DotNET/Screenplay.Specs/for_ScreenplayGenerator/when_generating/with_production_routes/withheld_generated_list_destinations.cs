// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class withheld_generated_list_destinations : a_generated_document
{
    [Theory]
    [InlineData("escape")]
    [InlineData("side-effect")]
    [InlineData("loop")]
    [InlineData("computed")]
    [InlineData("missing")]
    [InlineData("sequence")]
    [InlineData("reassigned")]
    [InlineData("ref")]
    [InlineData("unsupported-equals")]
    [InlineData("non-command-equals")]
    public void should_leave_unproven_list_productions_in_the_handler(string variant)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, GeneratedListSources.With(variant)));
            var command = Result.Model.Slices.SelectMany(slice => slice.Commands).Single();
            command.Produces.ShouldBeEmpty();
            command.Authoring.Identifier.ShouldBeNull();
            Result.Source.ShouldContain("handler");
            Result.Source.ShouldNotContain("produces when");
            Result.Source.ShouldNotContain(" generated identifier");
            Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
            AssertPartialDocument();
        }
    }
}

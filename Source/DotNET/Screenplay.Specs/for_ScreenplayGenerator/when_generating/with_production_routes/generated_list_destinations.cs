// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_production_routes;

public class generated_list_destinations : a_generated_document
{
    [Theory]
    [InlineData("supported")]
    [InlineData("alias")]
    [InlineData("unqualified-equals")]
    [InlineData("instance-equals")]
    public void should_recover_the_generated_destination_response_and_guard(string variant)
    {
        foreach (var authoring in new[] { false, true })
        {
            Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, GeneratedListSources.With(variant)));
            Result.Source.ShouldContain("id AuthorId generated identifier");
            Result.Source.ShouldContain("receipt Receipt generated");
            Result.Source.ShouldContain("produces when enabled == true");
            Result.Source.ShouldContain("for id");
            Result.Source.ShouldContain("returns receipt");
            Result.Model.Slices.SelectMany(slice => slice.Commands).Single().Produces.Count().ShouldEqual(2);
            Result.Diagnostics.ShouldBeEmpty();
            AssertDocument();
        }
    }
}

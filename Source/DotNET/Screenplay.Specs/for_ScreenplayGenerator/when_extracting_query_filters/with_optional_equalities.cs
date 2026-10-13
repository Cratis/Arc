// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_optional_equalities : given.a_generated_document
{
    [Theory]
    [InlineData("name is null || x.Name == name")]
    [InlineData("x.Name == name || name is null")]
    [InlineData("name == null || name == x.Name")]
    [InlineData("null == name || x.Name == name")]
    [InlineData("name == x.Name || null == name")]
    [InlineData("(name is null) || (x.Name == name)")]
    public void should_emit_only_null_skip_as_optional(string predicate)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create("IEnumerable<Item> items, string? name = null", $"=> items.Where(x => {predicate});")));
        Result.Source.ShouldContain("filter name String optional");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_scalar_property_types : given.a_generated_document
{
    [Theory]
    [InlineData("string? name", "x.Name == name", "filter name String")]
    [InlineData("string? NAME", "x.Name == NAME", "filter name String")]
    [InlineData("int count", "count == x.Count", "filter count Int")]
    [InlineData("Status? status", "status is null || status == x.Status", "filter status Status optional")]
    [InlineData("Category? category", "category is null || x.Category == category", "filter category Category optional")]
    public void should_use_the_property_type_and_only_proven_optionality(string input, string predicate, string filter)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create($"Guid id, {input}, IEnumerable<Item> items", $"=> items.Where(x => x.Id == id && ({predicate}));")));
        Result.Source.Split('\n').Select(line => line.Trim()).ShouldContain(filter);
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }
}

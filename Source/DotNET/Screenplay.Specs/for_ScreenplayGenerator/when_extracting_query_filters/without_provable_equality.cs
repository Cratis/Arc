// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class without_provable_equality : given.a_generated_document
{
    [Theory]
    [InlineData("string? title", "x.Name == title")]
    [InlineData("string? name", "x.Name != name")]
    [InlineData("int count", "x.Count < count")]
    [InlineData("int count", "x.Count > count")]
    [InlineData("string name", "x.Name!.Contains(name)")]
    [InlineData("string name", "x.Name == name.Trim()")]
    [InlineData("int count", "x.Count == count + 1")]
    [InlineData("string name", "x.Name == name && x.Name == name")]
    [InlineData("string name", "x.Name == name || x.Name == \"other\"")]
    [InlineData("string? name", "name is not null || x.Name == name")]
    [InlineData("string? name", "name != null && x.Name == name")]
    [InlineData("string? name", "x.Name == name || x.Id == id")]
    [InlineData("string? name", "!(x.Name != name)")]
    [InlineData("string? name", "x.Name == name && x.Category.Value == name")]
    [InlineData("long count", "x.Count == count")]
    [InlineData("string? name = \"default\"", "name is null || x.Name == name")]
    public void should_withhold_and_diagnose_unproven_parameters(string input, string predicate)
    {
        var source = given.QueryFilterSources.Create($"Guid id, IEnumerable<Item> items, {input}", $"=> items.Where(x => {predicate});");
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("filter ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("Status status, Status STATUS", "x.Status == status && x.Status == STATUS")]
    [InlineData("Guid ID", "x.Id == id && x.Id == ID")]
    public void should_withhold_filters_whose_emitted_names_collide(string input, string predicate)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create($"Guid id, {input}, IEnumerable<Item> items", $"=> items.Where(x => {predicate});")));
        Result.Source.ShouldNotContain("filter ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    [Theory]
    [InlineData("{ var unrelated = items.Where(x => x.Name == name); return items; }")]
    [InlineData("{ if (id == Guid.Empty) return items.Where(x => x.Name == name); return items; }")]
    [InlineData("=> items.Select(x => x.Name == name ? x : x);")]
    public void should_not_infer_a_filter_from_unreturned_or_conditional_predicates(string body)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create("Guid id, string? name, IEnumerable<Item> items", body)));
        Result.Source.ShouldNotContain("filter ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}

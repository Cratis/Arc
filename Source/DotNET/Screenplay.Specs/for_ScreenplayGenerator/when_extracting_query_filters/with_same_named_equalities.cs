// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_same_named_equalities : given.a_generated_document
{
    const string Signature = "Guid id, Status status, Category category, string? name, IEnumerable<Item> items";
    const string Body = "=> items.Where(x => x.Id == id && status == x.Status && x.Category == category && (name is null || x.Name == name));";

    void Because() => Generate((Analyzed.SlicePath, given.QueryFilterSources.Create(Signature, Body)));

    [Fact] void should_keep_the_existing_key() => Result.Source.ShouldContain("by id Uuid");
    [Fact] void should_emit_the_enum_filter() => Result.Source.ShouldContain("filter status Status");
    [Fact] void should_emit_the_property_concept() => Result.Source.ShouldContain("filter category Category");
    [Fact] void should_emit_the_nullable_null_skip_filter_as_optional() => Result.Source.ShouldContain("filter name String optional");
    [Fact] void should_preserve_parameter_order() => Result.Source.IndexOf("filter status", StringComparison.Ordinal).ShouldBeLessThan(Result.Source.IndexOf("filter category", StringComparison.Ordinal));
    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_select_esm_v14() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    [Fact] void should_report_no_unproven_parameter() => Result.Diagnostics.Where(diagnostic => diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeEmpty();
}

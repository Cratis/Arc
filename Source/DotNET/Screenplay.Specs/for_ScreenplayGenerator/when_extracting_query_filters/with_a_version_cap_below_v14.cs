// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_a_version_cap_below_v14 : given.a_generated_document
{
    const string Signature = "Guid id, Status status, IEnumerable<Item> items";
    const string Body = "=> items.Where(x => x.Id == id && x.Status == status);";

    void Because() => Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V13 }, (Analyzed.SlicePath, given.QueryFilterSources.Create(Signature, Body)));

    [Fact] void should_withhold_filters() => Result.Source.ShouldNotContain("filter ");
    [Fact] void should_keep_the_existing_key() => Result.Source.ShouldContain("by id Uuid");
    [Fact] void should_name_the_required_version_and_cap() => Result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains("requires ESM v14", StringComparison.Ordinal)).Message.ShouldContain("capped at ESM v13");
    [Fact] void should_locate_the_cap_at_the_implementation_file() => Result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains("requires ESM v14", StringComparison.Ordinal)).Location.ShouldEqual("Feature/Slice/Slice.cs");
    [Fact] void should_use_the_existing_unmappable_query_diagnostic() => Result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains("requires ESM v14", StringComparison.Ordinal)).Code.ShouldEqual(ScreenplayDiagnosticCodes.UnmappableQuery);
    [Fact] void should_keep_the_filter_free_document_on_its_previous_version() => Bound.Value!.Model.SemanticVersion.IsAtLeast(SemanticVersion.V14).ShouldBeFalse();
    [Fact] void should_round_trip_and_bind() => AssertDocument();

    [Fact]
    void should_apply_the_cap_in_authoring_mode_too()
    {
        var result = new ScreenplayGenerator().Generate(Analyzed.Compile((Analyzed.SlicePath, given.QueryFilterSources.Create(Signature, Body))), new() { MaximumExecutableModelVersion = SemanticVersion.V13, AuthoringOnlyConstructs = true });
        result.Source.ShouldNotContain("filter ");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("requires ESM v14", StringComparison.Ordinal)).ShouldBeTrue();
    }
}

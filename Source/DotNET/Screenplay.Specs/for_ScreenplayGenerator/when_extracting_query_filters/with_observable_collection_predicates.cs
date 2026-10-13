// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_observable_collection_predicates : given.a_generated_document
{
    [Theory]
    [InlineData("IMongoCollection<Item> items", "=> items.Observe(x => x.Id == id && (status == null || x.Status == status));")]
    [InlineData("IMongoCollection<Item> items", "=> items.Observe(x => x.Id == id && (status == null || x.Status == status), ignoreQueryContext: true);")]
    [InlineData("IMongoCollection<Item> items", "=> MongoCollectionExtensions.Observe(items, x => x.Id == id && (status == null || x.Status == status));")]
    [InlineData("DbSet<Item> items", "=> items.Observe(x => x.Id == id && (status == null || x.Status == status));")]
    [InlineData("DbSet<Item> items", "=> items.Observe(x => x.Id == id && (status == null || x.Status == status), ignoreQueryContext: true);")]
    public void should_prove_the_observable_collection_filter(string dependency, string body)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create($"Guid id, Status? status, {dependency}", body, "ISubject<IEnumerable<Item>>")));
        Result.Source.ShouldContain("query Filtered => observable Item[]");
        Result.Source.ShouldContain("by id Uuid");
        Result.Source.ShouldContain("filter status Status optional");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }

    [Theory]
    [InlineData("=> items.Observe(x => x.Id == id && x.Status != status);", "ISubject<IEnumerable<Item>>")]
    [InlineData("=> items.Observe(x => x.Id == id || x.Status == status);", "ISubject<IEnumerable<Item>>")]
    [InlineData("=> items.ObserveSingle(x => x.Id == id && x.Status == status);", "ISubject<Item?>")]
    [InlineData("=> OtherObservations.Observe(items, x => x.Id == id && x.Status == status);", "ISubject<IEnumerable<Item>>")]
    public void should_keep_the_existing_proof_boundaries(string body, string returnType)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create("Guid id, Status status, IMongoCollection<Item> items", body, returnType)));
        Result.Source.ShouldNotContain("filter ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}

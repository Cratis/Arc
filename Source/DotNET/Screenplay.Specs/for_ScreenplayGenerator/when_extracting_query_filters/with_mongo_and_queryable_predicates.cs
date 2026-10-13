// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_mongo_and_queryable_predicates : given.a_generated_document
{
    [Theory]
    [InlineData("IQueryable<Item> items", "=> items.Where(x => x.Id == id && x.Status == status).ToArray();")]
    [InlineData("IEnumerable<Item> items", "{ return items.Where((x) => x.Id == id && x.Status == status).ToList(); }")]
    [InlineData("IEnumerable<Item> items", "=> Enumerable.Where(items, x => x.Id == id && x.Status == status);")]
    [InlineData("IMongoCollection<Item> items", "=> items.Find(x => x.Id == id && x.Status == status).ToList();")]
    [InlineData("IQueryable<Item> items", "=> items.Where(x => x.Id == id).Where(x => x.Status == status);")]
    public void should_recover_returned_collection_predicates(string source, string body)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create($"Guid id, Status status, {source}", body)));
        Result.Source.ShouldContain("filter status Status");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_extracting_query_filters;

public class with_async_collection_materializers : given.a_generated_document
{
    [Theory]
    [InlineData("Task<IEnumerable<Item>>", "=> await items.Where(x => x.Id == id && x.Status == status).ToListAsync();", true)]
    [InlineData("Task<IEnumerable<Item>>", "{ return await items.Where(x => x.Id == id && x.Status == status).ToArrayAsync(); }", true)]
    [InlineData("Task<List<Item>>", "=> items.Where(x => x.Id == id && x.Status == status).ToListAsync();", false)]
    [InlineData("Task<Item[]>", "=> items.Where(x => x.Id == id && x.Status == status).ToArrayAsync();", false)]
    [InlineData("IAsyncEnumerable<Item>", "=> items.Where(x => x.Id == id && x.Status == status).AsAsyncEnumerable();", false)]
    public void should_preserve_the_proven_where_filter(string returnType, string body, bool isAsync)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create("Guid id, Status status, IQueryable<Item> items", body, returnType, isAsync)));
        Result.Source.ShouldContain("query Filtered => Item[]");
        Result.Source.ShouldContain("by id Uuid");
        Result.Source.ShouldContain("filter status Status");
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V14);
    }

    [Theory]
    [InlineData("=> await items.ToListAsync();")]
    [InlineData("=> await items.Where(x => x.Status != status).ToListAsync();")]
    [InlineData("=> await items.Where(x => x.Status == status || x.Id == id).ToArrayAsync();")]
    [InlineData("=> await OtherTerminals.ToListAsync(items.Where(x => x.Status == status));")]
    public void should_not_weaken_the_equality_or_terminal_proof(string body)
    {
        Generate((Analyzed.SlicePath, given.QueryFilterSources.Create("Guid id, Status status, IQueryable<Item> items", body, "Task<IEnumerable<Item>>", isAsync: true)));
        Result.Source.ShouldNotContain("filter ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableQuery && diagnostic.Message.Contains("not a proven", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }
}

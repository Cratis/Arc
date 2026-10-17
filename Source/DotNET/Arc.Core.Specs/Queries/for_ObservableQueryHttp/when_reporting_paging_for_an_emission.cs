// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_reporting_paging_for_an_emission : Specification
{
    [Theory]
    [InlineData("value", false)]
    [InlineData("string", false)]
    [InlineData("null", false)]
    [InlineData("collection", true)]
    public async Task should_only_report_paging_for_collections(string shape, bool isCollection)
    {
        var context = new QueryContext("TestQuery", CorrelationId.New(), new Paging(2, 10, true), Sorting.None)
        {
            TotalItems = 73
        };
        object? data = shape switch
        {
            "value" => 42,
            "string" => "result",
            "collection" => (int[])[1, 2],
            _ => null
        };
        var response = await ObservableQueryHttp.CreateResponse(
            context,
            Observable.Return(data),
            new ObservableQueryHttpOptions(true, TimeSpan.FromSeconds(1)),
            CancellationToken.None);

        response.Result.Paging.ShouldEqual(isCollection ? new PagingInfo(2, 10, 73) : PagingInfo.NotPaged);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.given;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_query_context_is_paged : a_paged_observable_connection
{
    [Theory]
    [InlineData("collection", true)]
    [InlineData("string", false)]
    [InlineData("value", false)]
    [InlineData("null", false)]
    public async Task should_only_report_paging_for_collection_emissions(string shape, bool isCollection)
    {
        var paging = await CapturePaging(EmittedValue(shape), useSse: true);
        paging.ShouldEqual(isCollection ? ExpectedPaging : PagingInfo.NotPaged);
    }
}

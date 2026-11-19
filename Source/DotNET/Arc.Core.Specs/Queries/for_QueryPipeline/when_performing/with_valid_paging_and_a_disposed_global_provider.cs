// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

[Collection("MutatesTypeDiscovery")]
public class with_valid_paging_and_a_disposed_global_provider : given.a_paged_query_with_a_disposed_global_provider
{
    async Task Because() => _result = await _pipeline.Perform("SomeQuery", [], _paging, Sorting.None, _serviceProvider);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_query_data() => _result.Data.ShouldEqual(_queryData);
    [Fact] void should_return_paging() => _result.Paging.ShouldEqual(new PagingInfo(1, 2, 3));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

[Collection("MutatesTypeDiscovery")]
public class with_invalid_paging_and_a_disposed_global_provider : given.a_paged_query_with_a_disposed_global_provider
{
    void Establish() => _paging = new Paging(0, -1, true);

    async Task Because() => _result = await _pipeline.Perform("SomeQuery", [], _paging, Sorting.None, _serviceProvider);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_return_the_paging_validation_error() => _result.ValidationResults.Single().Message.ShouldEqual("Page size must be greater than 0");
    [Fact] void should_identify_the_invalid_page_size() => _result.ValidationResults.Single().Members.ShouldContainOnly("size");
}

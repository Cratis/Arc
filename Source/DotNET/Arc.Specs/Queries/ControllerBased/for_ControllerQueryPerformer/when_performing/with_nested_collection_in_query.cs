// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_nested_collection_in_query : given.a_nested_collection_endpoint
{
    async Task Because() => await Invoke("QUERY", "Query", string.Empty, "{\"arguments\":{\"values\":[[1,2]]}}");

    [Fact] void should_classify_the_collection_as_a_query_argument() => _performer.Parameters.Any(parameter => parameter.Name == "values").ShouldBeTrue();
    [Fact] void should_return_http_400() => _httpContext.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_return_a_validation_failure() => _response.RootElement.GetProperty("validationResults").GetArrayLength().ShouldEqual(1);
    [Fact] void should_not_invoke_the_action() => NestedCollectionController.WasCalled.ShouldBeFalse();
}

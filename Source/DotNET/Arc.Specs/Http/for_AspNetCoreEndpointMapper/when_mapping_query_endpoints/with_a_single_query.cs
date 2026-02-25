// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_query_endpoints;

public class with_a_single_query : given.a_query_endpoint
{
    IReadOnlyList<RouteEndpoint> _endpoints;
    RouteEndpoint _queryEndpoint;

    void Because()
    {
        _mapper.MapQueryEndpoints(_app.Services);

        _endpoints = GetRouteEndpoints().ToList();
        _queryEndpoint = FindEndpointByName("ExecuteAllOrders");
    }

    [Fact] void should_register_the_query_endpoint() => _queryEndpoint.ShouldNotBeNull();
    [Fact] void should_have_method_info_on_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_use_get_for_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("GET");
    [Fact] void should_register_one_endpoint() => _endpoints.Count.ShouldEqual(1);
    [Fact] void should_produce_json_on_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<IProducesResponseTypeMetadata>().Type.ShouldEqual(typeof(QueryResult));
}

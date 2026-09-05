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
    RouteEndpoint _queryMethodEndpoint;

    void Because()
    {
        _mapper.MapQueryEndpoints(_app.Services);

        _endpoints = GetRouteEndpoints().ToList();
        _queryEndpoint = FindEndpointByName("ExecuteFeatures.Orders.AllOrders");
        _queryMethodEndpoint = FindEndpointByName("QueryFeatures.Orders.AllOrders");
    }

    [Fact] void should_register_the_query_endpoint() => _queryEndpoint.ShouldNotBeNull();
    [Fact] void should_have_method_info_on_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_use_get_for_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("GET");
    [Fact] void should_register_a_get_and_a_query_endpoint() => _endpoints.Count.ShouldEqual(2);
    [Fact] void should_produce_json_on_query_endpoint() => _queryEndpoint.Metadata.GetMetadata<IProducesResponseTypeMetadata>().Type.ShouldEqual(typeof(QueryResult));
    [Fact] void should_register_the_query_method_endpoint() => _queryMethodEndpoint.ShouldNotBeNull();
    [Fact] void should_use_query_for_query_method_endpoint() => _queryMethodEndpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("QUERY");
    [Fact] void should_exclude_query_method_endpoint_from_api_description() => _queryMethodEndpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>().ExcludeFromDescription.ShouldBeTrue();
}

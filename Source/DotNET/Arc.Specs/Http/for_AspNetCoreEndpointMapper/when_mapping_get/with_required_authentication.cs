// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_get;

public class with_required_authentication : given.an_endpoint_mapper
{
    RouteEndpoint _endpoint;

    void Establish() => _mapper.MapGet("/catalog", _ => Task.CompletedTask, new EndpointMetadata("Catalog") { RequireAuthentication = true, Roles = "Administrator,Operator" });

    void Because() => _endpoint = FindEndpoint("/catalog");

    [Fact] void should_require_authentication() => _endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ShouldNotBeEmpty();
    [Fact] void should_require_either_role() => _endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Single().Roles.ShouldEqual("Administrator,Operator");
    [Fact] void should_not_allow_anonymous() => _endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldBeNull();
}

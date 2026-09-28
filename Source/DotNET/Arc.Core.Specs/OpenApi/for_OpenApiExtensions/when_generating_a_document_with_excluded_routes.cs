// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.OpenApi.for_OpenApiExtensions;

public class when_generating_a_document_with_excluded_routes : Specification
{
    Dictionary<string, object> _paths;

    void Because()
    {
        RouteInfo[] routes = [
            new RouteInfo("POST", "/hidden", new EndpointMetadata("Hidden", "Hidden", [], false, ExcludeFromApiDescription: true)),
            new RouteInfo("GET", "/visible", new EndpointMetadata("Visible", "Visible", [], false))
        ];
        var document = OpenApiExtensions.GenerateOpenApiDocument(routes, "API", "1.0");
        _paths = (Dictionary<string, object>)document["paths"];
    }

    [Fact] void should_not_describe_the_hidden_route() => _paths.ContainsKey("/hidden").ShouldBeFalse();
    [Fact] void should_describe_the_visible_route() => _paths.ContainsKey("/visible").ShouldBeTrue();
}

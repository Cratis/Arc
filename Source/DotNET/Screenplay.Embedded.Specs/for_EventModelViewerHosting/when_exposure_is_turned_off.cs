// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_exposure_is_turned_off : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;
    HttpResponseMessage _index;
    HttpResponseMessage _asset;

    protected override bool? Exposure => false;

    async Task Because()
    {
        _hierarchy = await _client.GetAsync(Url("/hierarchy"));
        _index = await _client.GetAsync(Url("/"));
        _asset = await _client.GetAsync(Url("/assets/index.js"));
    }

    [Fact] void should_serve_no_hierarchy() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_serve_no_explorer() => _index.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_serve_no_assets() => _asset.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_map_nothing() => EndpointsForTheExplorer().ShouldEqual(0);
}

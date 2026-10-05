// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_enabled_explicitly_in_staging : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;

    protected override string EnvironmentName => Environments.Staging;

    async Task Because() => _hierarchy = await _client.GetAsync(Url("/hierarchy"));

    [Fact] void should_serve_the_automatically_mapped_explorer() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_map_the_explorer_once() => EndpointsForTheExplorer().ShouldEqual(5);
}

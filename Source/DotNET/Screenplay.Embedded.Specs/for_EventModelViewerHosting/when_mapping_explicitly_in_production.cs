// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_mapping_explicitly_in_production : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;

    protected override string EnvironmentName => Environments.Production;

    protected override bool? Exposure => null;

    protected override bool MapsTheExplorerExplicitly => true;

    protected override IReadOnlyList<Assembly> ExplicitlyMappedAssemblies => ConfiguredAssemblies;

    async Task Because() => _hierarchy = await _client.GetAsync(Url("/hierarchy"));

    [Fact] void should_serve_the_explicitly_mapped_explorer() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_map_the_explorer_once() => EndpointsForTheExplorer().ShouldEqual(5);
}

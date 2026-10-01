// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_the_application_is_exposed_automatically : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;
    HttpResponseMessage _index;
    JsonElement _projects;

    async Task Because()
    {
        _hierarchy = await _client.GetAsync(Url("/hierarchy"));
        _index = await _client.GetAsync(Url("/"));
        _projects = JsonDocument.Parse(await _hierarchy.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact] void should_serve_the_hierarchy() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_explorer() => _index.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_documents_of_the_application() => _projects.GetArrayLength().ShouldEqual(1);
    [Fact] void should_name_the_project_after_the_application_assembly() => _projects[0].GetProperty("id").GetString().ShouldEqual("EmbeddedConsumer");
    [Fact] void should_map_the_explorer_once() => EndpointsForTheExplorer().ShouldEqual(5);
}

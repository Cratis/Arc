// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_the_host_maps_the_explorer_as_well : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;
    HttpResponseMessage _source;
    string _sourceText;
    JsonElement _projects;

    protected override bool MapsTheExplorerExplicitly => true;

    protected override IReadOnlyList<Assembly> ExplicitlyMappedAssemblies => [an_embedded_application.Assembly];

    async Task Because()
    {
        _hierarchy = await _client.GetAsync(Url("/hierarchy"));
        _source = await _client.GetAsync(Url($"/documents/{an_embedded_application.ProjectId}/{an_embedded_application.ApplicationDocumentId}/source"));
        _sourceText = await _source.Content.ReadAsStringAsync();
        _projects = JsonDocument.Parse(await _hierarchy.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact] void should_answer_rather_than_find_the_route_ambiguous() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_map_the_explorer_once() => EndpointsForTheExplorer().ShouldEqual(5);
    [Fact] void should_serve_what_both_calls_asked_for() => _projects.GetArrayLength().ShouldEqual(2);
    [Fact] void should_serve_the_documents_the_host_mapped_itself() => _source.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_source_the_host_mapped_itself() => _sourceText.ShouldEqual(an_embedded_application.ApplicationSource);
}

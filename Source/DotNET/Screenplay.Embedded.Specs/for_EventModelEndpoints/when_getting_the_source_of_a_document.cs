// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_the_source_of_a_document : a_host
{
    HttpResponseMessage _response;
    string _source;

    async Task Because()
    {
        _response = await _client.GetAsync(Url($"/documents/{an_embedded_application.ProjectId}/{an_embedded_application.ModuleDocumentId}/source"));
        _source = await _response.Content.ReadAsStringAsync();
    }

    [Fact] void should_not_allow_caching_application_structure() => _response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    [Fact] void should_answer_with_the_source() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_answer_as_text() => _response.Content.Headers.ContentType!.MediaType.ShouldEqual("text/plain");
    [Fact] void should_serve_the_resource_the_catalog_names() => _source.ShouldEqual(an_embedded_application.ModuleSource);
}

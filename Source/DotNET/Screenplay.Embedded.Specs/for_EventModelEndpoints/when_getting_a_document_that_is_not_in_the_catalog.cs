// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_a_document_that_is_not_in_the_catalog : a_host
{
    HttpResponseMessage _unknownDocumentSource;
    HttpResponseMessage _unknownDocumentModel;
    HttpResponseMessage _unknownProjectSource;
    HttpResponseMessage _unknownProjectModel;

    async Task Because()
    {
        _unknownDocumentSource = await _client.GetAsync(Url($"/documents/{an_embedded_application.ProjectId}/Nothing.Here/source"));
        _unknownDocumentModel = await _client.GetAsync(Url($"/documents/{an_embedded_application.ProjectId}/Nothing.Here/model"));
        _unknownProjectSource = await _client.GetAsync(Url($"/documents/Nothing.Here/{an_embedded_application.ApplicationDocumentId}/source"));
        _unknownProjectModel = await _client.GetAsync(Url($"/documents/Nothing.Here/{an_embedded_application.ApplicationDocumentId}/model"));
    }

    [Fact] void should_not_find_the_source_of_an_unknown_document() => _unknownDocumentSource.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_find_the_model_of_an_unknown_document() => _unknownDocumentModel.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_find_the_source_in_an_unknown_project() => _unknownProjectSource.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_find_the_model_in_an_unknown_project() => _unknownProjectModel.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}

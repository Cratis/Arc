// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelViewerHosting;

public class when_authorization_is_required_automatically : an_automatically_hosted_application
{
    HttpResponseMessage _hierarchy;
    HttpResponseMessage _index;
    HttpResponseMessage _source;
    HttpResponseMessage _model;
    HttpResponseMessage _asset;

    protected override bool RequiresAuthorization => true;

    async Task Because()
    {
        _hierarchy = await _client.GetAsync(Url("/hierarchy"));
        _index = await _client.GetAsync(Url("/"));
        _source = await _client.GetAsync(Url("/documents/EmbeddedConsumer/Company.Library/source"));
        _model = await _client.GetAsync(Url("/documents/EmbeddedConsumer/Company.Library/model"));
        _asset = await _client.GetAsync(Url("/assets/index.js"));
    }

    [Fact] void should_not_serve_the_hierarchy_to_anyone_unauthorized() => _hierarchy.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_serve_the_explorer_to_anyone_unauthorized() => _index.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_serve_the_source_to_anyone_unauthorized() => _source.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_serve_the_model_to_anyone_unauthorized() => _model.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_serve_the_assets_to_anyone_unauthorized() => _asset.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}

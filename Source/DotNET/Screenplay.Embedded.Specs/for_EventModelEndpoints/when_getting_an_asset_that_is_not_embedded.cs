// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_an_asset_that_is_not_embedded : a_host
{
    HttpResponseMessage _unknown;
    HttpResponseMessage _traversal;
    HttpResponseMessage _encodedTraversal;

    async Task Because()
    {
        _unknown = await _client.GetAsync(Url("/assets/nothing-here.js"));
        _traversal = await _client.GetAsync(Url("/assets/../../../appsettings.json"));
        _encodedTraversal = await _client.GetAsync(Url("/assets/%2E%2E%2F%2E%2E%2Fappsettings.json"));
    }

    [Fact] void should_not_find_an_asset_that_was_never_packaged() => _unknown.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_anything_a_traversal_names() => _traversal.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_anything_an_encoded_traversal_names() => _encodedTraversal.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}

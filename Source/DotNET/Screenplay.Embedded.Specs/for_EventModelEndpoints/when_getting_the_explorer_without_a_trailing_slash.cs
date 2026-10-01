// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_the_explorer_without_a_trailing_slash : a_host
{
    HttpResponseMessage _response;

    protected override string? PathBase => "/behind";

    async Task Because() => _response = await _client.GetAsync(Url(string.Empty));

    [Fact] void should_redirect_to_the_path_the_assets_resolve_against() => _response.StatusCode.ShouldEqual(HttpStatusCode.Found);
    [Fact] void should_keep_the_path_base_of_the_host_in_the_location() => _response.Headers.Location!.OriginalString.ShouldEqual("/behind/.cratis/event-model/");
}

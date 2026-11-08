// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_serving_metadata_for_tokens_only : an_mcp_host
{
    McpResponse _metadata;

    async Task Because()
    {
        Unauthenticate();
        _metadata = await Get("/.well-known/oauth-protected-resource/mcp");
    }

    [Fact] void should_not_advertise_oauth() => _metadata.Status.ShouldEqual(HttpStatusCode.NotFound);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Mcp.for_McpOptIn.given;

namespace Cratis.Arc.Mcp.for_McpOptIn;

public class when_mcp_is_not_enabled : an_opt_in_host
{
    HttpStatusCode _status;

    void Establish() => Build(false);

    async Task Because()
    {
        await _app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient();
        using var response = await client.PostAsync($"{_app.Urls.Single()}/mcp", new StringContent("{}"), TestContext.Current.CancellationToken);
        _status = response.StatusCode;
    }

    [Fact] void should_serve_no_mcp_route() => _status.ShouldEqual(HttpStatusCode.NotFound);
}

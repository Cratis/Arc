// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Samples.ArcCore.Specs.for_PublishedArcCore.given;

public class an_authenticated_query_request : a_published_arc_core
{
    const string ForgedPrincipal = /*lang=json,strict*/ """{"userId":"forged-3657","userDetails":"forged","userRoles":["admin"],"claims":[]}""";
    protected HttpStatusCode _statusCode;

    protected async Task GetAuthenticatedWithForgedCookie()
    {
        if (_arcCore is null)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/features/authentication-queries/authenticated");
        request.Headers.Add("Cookie", $"x-ms-client-principal={Convert.ToBase64String(Encoding.UTF8.GetBytes(ForgedPrincipal))}");

        // The query is observable, so only wait for the headers instead of the (endless) body.
        using var response = await _arcCore.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        _statusCode = response.StatusCode;
    }
}

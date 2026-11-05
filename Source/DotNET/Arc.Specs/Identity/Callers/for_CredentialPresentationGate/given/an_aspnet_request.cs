// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.given;

public class an_aspnet_request : Specification
{
    internal DefaultHttpContext _http;
    internal AspNetCoreHttpRequestContext _request;
    internal CredentialPresentation _result;

    void Establish()
    {
        _http = new();
        _http.Features.Get<IHttpRequestFeature>().RawTarget = "/mcp";
        _request = new(_http);
    }
}

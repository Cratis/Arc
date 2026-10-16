// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.given;

public class an_http_connection_resolver : Specification
{
    protected Uri _endpoint;
    protected HttpConnectionResolverOptions _options;
    protected ConnectionRequest _request;
    protected HttpResponseMessage _response;
    protected HttpRequestMessage? _sentRequest;
    protected string _sentBody = string.Empty;
    protected HttpConnectionResolver _resolver;

    void Establish()
    {
        _endpoint = new("https://platform.example/connections");
        _options = new()
        {
            Endpoint = _endpoint,
            Headers = new Dictionary<string, string> { ["X-Api-Key"] = "the-key" }
        };
        _request = new(
            WellKnownConnectionStorage.SqlServer,
            ConnectionPurpose.Runtime,
            new Dictionary<string, string> { [WellKnownConnectionScopeKeys.Tenant] = "acme" });
        _response = new(HttpStatusCode.NoContent);
        _resolver = new(_options, new HttpClient(new Handler(this)));
    }

    sealed class Handler(an_http_connection_resolver context) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            context._sentRequest = request;
            context._sentBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return context._response;
        }
    }
}

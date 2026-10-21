// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Arc.Connections.for_ClientCredentialsAccessTokenProvider.given;

public class a_client_credentials_provider : Specification
{
    protected DateTimeOffset _now;
    protected HttpConnectionResolverAuthenticationOptions _options;
    protected HttpStatusCode _status = HttpStatusCode.OK;
    protected string _answer = """{ "access_token": "the-token", "token_type": "Bearer", "expires_in": 3599 }""";
    protected HttpRequestMessage? _sentRequest;
    protected string _sentBody = string.Empty;
    protected ClientCredentialsAccessTokenProvider _provider;

    void Establish()
    {
        _now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        _options = new()
        {
            Type = HttpConnectionResolverAuthenticationType.ClientCredentials,
            TokenEndpoint = new("https://login.example/tenant/oauth2/v2.0/token"),
            ClientId = "the-client",
            ClientSecret = "the-secret",
            Scope = "api://platform/.default"
        };
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(_now);
        _provider = new(new HttpClient(new Handler(this)), time);
    }

    sealed class Handler(a_client_credentials_provider context) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            context._sentRequest = request;
            context._sentBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(context._status) { Content = new StringContent(context._answer, Encoding.UTF8, "application/json") };
        }
    }
}

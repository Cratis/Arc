// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.given;

public class an_authenticated_http_connection_resolver : Specification
{
    protected HttpConnectionResolverOptions _options;
    protected ConnectionRequest _request;
    protected Queue<HttpStatusCode> _statuses = new();
    protected List<HttpRequestMessage> _sentRequests = [];
    protected IConnectionResolverAccessTokenProvider _provider;
    protected int _tokensIssued;
    protected HttpConnectionResolver _resolver;

    void Establish()
    {
        _options = new()
        {
            Endpoint = new("https://platform.example/connections"),
            Headers = new Dictionary<string, string> { ["X-Api-Key"] = "the-key" },
            Authentication = new() { Type = HttpConnectionResolverAuthenticationType.ClientCredentials }
        };
        _request = new(
            WellKnownConnectionStorage.SqlServer,
            ConnectionPurpose.Runtime,
            new Dictionary<string, string> { [WellKnownConnectionScopeKeys.Tenant] = "acme" });
        _provider = Substitute.For<IConnectionResolverAccessTokenProvider>();
        _provider.CanProvide(Arg.Any<HttpConnectionResolverAuthenticationOptions>()).Returns(true);
        _provider.GetToken(Arg.Any<HttpConnectionResolverAuthenticationOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ConnectionResolverAccessToken>(new ConnectionResolverAccessToken($"token-{++_tokensIssued}", DateTimeOffset.UtcNow.AddHours(1))));
        _resolver = new(_options, new HttpClient(new Handler(this)), [_provider], TimeProvider.System);
    }

    protected string? AuthorizationOf(int request) => _sentRequests[request].Headers.Authorization?.ToString();

    sealed class Handler(an_authenticated_http_connection_resolver context) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            context._sentRequests.Add(request);
            var status = context._statuses.Count > 0 ? context._statuses.Dequeue() : HttpStatusCode.NoContent;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}

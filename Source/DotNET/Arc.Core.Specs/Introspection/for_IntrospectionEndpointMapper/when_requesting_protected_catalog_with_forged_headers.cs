// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Cratis.Arc.Http.for_HttpListenerEndpointMapper.given;
using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_requesting_protected_catalog_with_forged_headers : a_running_endpoint_mapper
{
    HttpStatusCode _status;

    async Task Because()
    {
        var options = new ArcOptions { Introspection = new() { RequireAuthentication = true } };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<IAuthentication>(new HeaderAuthentication(new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(options), NullLoggerFactory.Instance)));
        _serviceProvider = services.BuildServiceProvider();
        _endpointMapper.MapIntrospectionEndpoints(options.Introspection);
        StartEndpointMapper();

        var principal = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            userId = "forged",
            userDetails = "attacker@example.com",
            userRoles = new[] { "Administrator" },
            claims = Array.Empty<object>()
        }));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.cratis/commands");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityIdHeader, "forged");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, "attacker");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.PrincipalHeader, principal);
        using var response = await _httpClient.SendAsync(request);
        _status = response.StatusCode;
    }

    [Fact] void should_refuse_forged_identity() => _status.ShouldEqual(HttpStatusCode.Unauthorized);

    class HeaderAuthentication(MicrosoftIdentityPlatformAuthenticationHandler handler) : IAuthentication
    {
        public bool HasHandlers => true;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) => handler.HandleAuthentication(context);
    }
}

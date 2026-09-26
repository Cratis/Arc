// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_a_route_group_authenticates_with_unsigned_headers : Specification
{
    HttpStatusCode _commandsStatus;
    HttpStatusCode _queriesStatus;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication("Clean")
            .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("Clean", _ => { })
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>("Headers", _ => { });
        builder.Services.AddAuthorization();
        builder.AddCratisArc();
        await using var app = builder.Build();
        var group = app.MapGroup("/group").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Headers" });
        new AspNetCoreEndpointMapper(group).MapIntrospectionEndpoints(new IntrospectionOptions { RequireAuthentication = true });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var principal = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            userId = "forged",
            userDetails = "attacker@example.com",
            userRoles = new[] { "Administrator" },
            claims = Array.Empty<object>()
        }));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/group/.cratis/commands");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityIdHeader, "forged");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, "attacker");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.PrincipalHeader, principal);
        using var response = await client.SendAsync(request);
        _commandsStatus = response.StatusCode;
        using var queryRequest = new HttpRequestMessage(HttpMethod.Get, "/group/.cratis/queries");
        queryRequest.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityIdHeader, "forged");
        queryRequest.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, "attacker");
        queryRequest.Headers.Add(MicrosoftIdentityPlatformHeaders.PrincipalHeader, principal);
        using var queryResponse = await client.SendAsync(queryRequest);
        _queriesStatus = queryResponse.StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_challenge_the_command_catalog_request() => _commandsStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_challenge_the_query_catalog_request() => _queriesStatus.ShouldEqual(HttpStatusCode.Unauthorized);
}

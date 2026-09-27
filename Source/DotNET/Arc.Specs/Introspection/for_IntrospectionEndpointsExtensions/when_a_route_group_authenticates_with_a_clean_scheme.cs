// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_a_route_group_authenticates_with_a_clean_scheme : Specification
{
    HttpStatusCode _status;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication("Clean")
            .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("Clean", _ => { })
            .AddScheme<AuthenticationSchemeOptions, Identity.MicrosoftIDentityPlatformAuthHandler>("Headers", _ => { });
        builder.Services.AddAuthorization();
        builder.AddCratisArc();
        await using var app = builder.Build();
        var group = app.MapGroup("/group").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Clean,Headers" });
        new AspNetCoreEndpointMapper(group).MapIntrospectionEndpoints(new IntrospectionOptions { RequireAuthentication = true });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/group/.cratis/commands");
        request.Headers.Add("X-Test-User", "legitimate");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_serve_the_catalog() => _status.ShouldEqual(HttpStatusCode.OK);
}

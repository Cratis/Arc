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
public class when_headers_authenticate_before_routing : Specification
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

        // Simulate authentication before UseRouting: the handler cannot inspect endpoint metadata yet.
        app.Use(async (context, next) =>
        {
            var result = await context.AuthenticateAsync("Headers");
            if (result.Succeeded)
            {
                context.User = result.Principal!;
            }
            await next();
        });
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        var group = app.MapGroup("/group").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Headers" });
        new AspNetCoreEndpointMapper(group).MapIntrospectionEndpoints(new IntrospectionOptions { RequireAuthentication = true });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var request = given.forged_catalog_request.Create("/group/.cratis/commands");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_reject_the_authenticated_request_despite_cached_authentication() => _status.ShouldEqual(HttpStatusCode.Unauthorized);
}

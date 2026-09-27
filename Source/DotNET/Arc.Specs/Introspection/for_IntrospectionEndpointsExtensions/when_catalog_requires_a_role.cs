// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_catalog_requires_a_role : Specification
{
    HttpStatusCode _anonymousStatus;
    HttpStatusCode _unauthorizedRoleStatus;
    HttpStatusCode _authorizedStatus;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication("CatalogSpec")
            .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("CatalogSpec", _ => { });
        builder.Services.AddAuthorization();
        builder.AddCratisArc(options =>
        {
            options.Introspection.RequireAuthentication = true;
            options.Introspection.Roles = "Administrator";
        });
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        _anonymousStatus = (await client.GetAsync("/.cratis/commands")).StatusCode;
        client.DefaultRequestHeaders.Add("X-Test-User", "someone");
        _unauthorizedRoleStatus = (await client.GetAsync("/.cratis/queries")).StatusCode;
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        _authorizedStatus = (await client.GetAsync("/.cratis/commands")).StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_reject_anonymous_requests() => _anonymousStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_reject_callers_without_the_role() => _unauthorizedRoleStatus.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_serve_callers_with_the_role() => _authorizedStatus.ShouldEqual(HttpStatusCode.OK);
}

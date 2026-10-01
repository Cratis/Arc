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
public class when_identity_discovery_requires_authentication : Specification
{
    HttpStatusCode _anonymousUsersStatus;
    HttpStatusCode _anonymousTenantsStatus;
    HttpStatusCode _anonymousSchemaStatus;
    HttpStatusCode _authenticatedUsersStatus;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication("CatalogSpec")
            .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("CatalogSpec", _ => { });
        builder.Services.AddAuthorization();
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = true);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        _anonymousUsersStatus = (await client.GetAsync("/.cratis/users")).StatusCode;
        _anonymousTenantsStatus = (await client.GetAsync("/.cratis/tenants")).StatusCode;
        _anonymousSchemaStatus = (await client.GetAsync("/.cratis/identity-details/schema")).StatusCode;
        client.DefaultRequestHeaders.Add("X-Test-User", "someone");
        _authenticatedUsersStatus = (await client.GetAsync("/.cratis/users")).StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_reject_anonymous_user_discovery() => _anonymousUsersStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_reject_anonymous_tenant_discovery() => _anonymousTenantsStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_reject_anonymous_identity_schema_requests() => _anonymousSchemaStatus.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_serve_authenticated_callers() => _authenticatedUsersStatus.ShouldEqual(HttpStatusCode.OK);
}

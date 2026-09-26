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
public class when_default_policy_allows_anonymous : Specification
{
    HttpStatusCode _status;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication("CatalogSpec")
            .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("CatalogSpec", _ => { });
        builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = true);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        _status = (await client.GetAsync("/.cratis/commands")).StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_reject_anonymous_despite_permissive_default_policy() => _status.ShouldEqual(HttpStatusCode.Unauthorized);
}

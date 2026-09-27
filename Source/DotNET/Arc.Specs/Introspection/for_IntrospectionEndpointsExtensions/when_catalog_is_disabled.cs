// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_catalog_is_disabled : Specification
{
    HttpStatusCode _commandsStatus;
    HttpStatusCode _queriesStatus;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.AddCratisArc(options => options.Introspection.Enabled = false);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        _commandsStatus = (await client.GetAsync("/.cratis/commands")).StatusCode;
        _queriesStatus = (await client.GetAsync("/.cratis/queries")).StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_not_serve_commands() => _commandsStatus.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_queries() => _queriesStatus.ShouldEqual(HttpStatusCode.NotFound);
}

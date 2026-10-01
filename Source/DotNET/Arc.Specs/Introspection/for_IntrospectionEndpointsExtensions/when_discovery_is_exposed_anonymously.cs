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
public class when_discovery_is_exposed_anonymously : Specification
{
    HttpStatusCode _usersStatus;
    HttpStatusCode _commandsStatus;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = false);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        _usersStatus = (await client.GetAsync("/.cratis/users")).StatusCode;
        _commandsStatus = (await client.GetAsync("/.cratis/commands")).StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_serve_user_discovery() => _usersStatus.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_serve_the_command_catalog() => _commandsStatus.ShouldEqual(HttpStatusCode.OK);
}

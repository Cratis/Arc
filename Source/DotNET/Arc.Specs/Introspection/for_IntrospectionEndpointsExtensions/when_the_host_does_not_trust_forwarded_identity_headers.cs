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
public class when_the_host_does_not_trust_forwarded_identity_headers : Specification
{
    HttpStatusCode _status;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
        builder.Services.AddAuthorization();
        builder.AddCratisArc(options => options.Introspection.RequireAuthentication = true);
        await using var app = builder.Build();
        app.UseCratisArc();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var request = given.forged_catalog_request.Create("/.cratis/commands");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        await app.StopAsync();
    }

    [Fact] void should_treat_the_forged_identity_as_anonymous() => _status.ShouldEqual(HttpStatusCode.Unauthorized);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_starting_capability_hosting_without_an_installation : Specification
{
    Exception? _error;

    async Task Because()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", port), configureBuilder: arc => arc.WithoutControllers().WithCapabilities());
        await using var app = builder.Build();
        app.UseCratisArc();
        _error = await Catch.Exception(() => app.StartAsync(global::Xunit.TestContext.Current.CancellationToken));
    }

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_installation() => _error!.Message.ShouldContain("the installation names no resource and environment");
}

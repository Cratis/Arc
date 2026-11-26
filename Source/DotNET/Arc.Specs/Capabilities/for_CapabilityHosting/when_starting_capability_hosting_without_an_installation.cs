// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_starting_capability_hosting_without_an_installation : Specification
{
    Exception? _error;

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await using var app = await KestrelPorts.Start(
                1,
                ports =>
            {
                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls($"http://127.0.0.1:{ports[0]}");
                builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", ports[0]), configureBuilder: arc => arc.WithoutControllers().WithCapabilities());
                var host = builder.Build();
                host.UseCratisArc();
                return host;
            },
                global::Xunit.TestContext.Current.CancellationToken);
        });
    }

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_installation() => _error!.Message.ShouldContain("the installation names no resource and environment");
}

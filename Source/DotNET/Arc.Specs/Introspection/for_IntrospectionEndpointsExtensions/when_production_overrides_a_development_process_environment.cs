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
public class when_production_overrides_a_development_process_environment : Specification
{
    HttpStatusCode[] _statuses;

    async Task Because()
    {
        var previous = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services.AddAuthentication("CatalogSpec")
                .AddScheme<AuthenticationSchemeOptions, given.catalog_authentication_handler>("CatalogSpec", _ => { });
            builder.Services.AddAuthorization();
            builder.AddCratisArc();
            await using var app = builder.Build();
            app.UseCratisArc();
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
                var statuses = new List<HttpStatusCode>();
                foreach (var path in new[] { "/.cratis/commands", "/.cratis/queries", "/.cratis/users", "/.cratis/tenants", "/.cratis/identity-details/schema" })
                {
                    using var response = await client.GetAsync(path);
                    statuses.Add(response.StatusCode);
                }
                _statuses = [.. statuses];
            }
            finally
            {
                await app.StopAsync();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previous);
        }
    }

    [Fact] void should_require_authentication_on_all_discovery_routes() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.Unauthorized, 5).ToArray());
}

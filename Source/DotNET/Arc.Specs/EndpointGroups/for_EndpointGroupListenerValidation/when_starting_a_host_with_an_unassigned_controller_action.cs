// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation;

public class when_starting_a_host_with_an_unassigned_controller_action : Specification
{
    WebApplication _app;
    Exception? _error;
    Endpoint[] _groupedEndpoints;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddOptions<ArcOptions>();
        builder.Services.AddSingleton<IHostedService, EndpointGroupListenerValidation>();
        builder.Services.AddControllers(options => options.Conventions.Add(new EndpointGroupControllerConvention()))
            .AddApplicationPart(typeof(given.ActionGroupedController).Assembly);
        _app = builder.Build();
        _app.MapControllers();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _app.StartAsync());
        _groupedEndpoints = _app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(e => e.Metadata.GetMetadata<EndpointGroupMetadata>()?.Name == "internal").ToArray();
    }

    [Fact] void should_materialize_grouped_mvc_actions_before_startup_validation() => _groupedEndpoints.Any(e => e.DisplayName?.Contains(nameof(given.ActionGroupedController), StringComparison.Ordinal) == true).ShouldBeTrue();
    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<EndpointGroupHasNoPort>();

    async Task Destroy() => await _app.DisposeAsync();
}

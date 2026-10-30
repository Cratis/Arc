// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation;

public class when_a_grouped_endpoint_has_a_port : Specification
{
    Exception? _error;

    async Task Because()
    {
        var addresses = new ServerAddressesFeature();
        addresses.Addresses.Add("http://localhost:5000");
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(addresses);
        var server = Substitute.For<IServer>();
        server.Features.Returns(features);
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new EndpointGroupMetadata("internal")), "App.InternalController.Get");
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IServer)).Returns(server);
        services.GetService(typeof(EndpointDataSource)).Returns(new DefaultEndpointDataSource(endpoint));
        var options = new ArcOptions();
        options.EndpointGroups.Assign("internal", 5001);
        var validation = new EndpointGroupListenerValidation(services, Options.Create(options));
        _error = await Catch.Exception(() => validation.StartedAsync(CancellationToken.None));
    }

    [Fact] void should_allow_startup() => _error.ShouldBeNull();
}

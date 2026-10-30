// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation;

public class when_a_grouped_endpoint_has_no_port : Specification
{
    Exception? _error;

    async Task Because()
    {
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new EndpointGroupMetadata("internal")), "App.InternalController.Get");
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(EndpointDataSource)).Returns(new DefaultEndpointDataSource(endpoint));
        var validation = new EndpointGroupListenerValidation(services, Options.Create(new ArcOptions()));
        _error = await Catch.Exception(() => validation.StartedAsync(CancellationToken.None));
    }

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<EndpointGroupHasNoPort>();
    [Fact] void should_name_the_endpoint() => _error!.Message.ShouldContain("App.InternalController.Get");
}

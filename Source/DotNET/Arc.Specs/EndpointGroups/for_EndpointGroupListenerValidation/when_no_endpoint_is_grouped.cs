// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation;

public class when_no_endpoint_is_grouped : Specification
{
    Exception? _error;

    async Task Because()
    {
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "App.PublicController.Get");
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(EndpointDataSource)).Returns(new DefaultEndpointDataSource(endpoint));
        var validation = new EndpointGroupListenerValidation(services, Options.Create(new ArcOptions()));
        _error = await Catch.Exception(() => validation.StartedAsync(CancellationToken.None));
    }

    [Fact] void should_allow_startup() => _error.ShouldBeNull();
}

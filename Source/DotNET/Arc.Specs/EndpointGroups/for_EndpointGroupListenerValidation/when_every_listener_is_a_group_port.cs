// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupListenerValidation;

public class when_every_listener_is_a_group_port : Specification
{
    EndpointGroupListenerValidation _validation;
    Exception? _error;

    void Establish()
    {
        var addresses = new ServerAddressesFeature();
        addresses.Addresses.Add("http://localhost:5001");
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(addresses);
        var server = Substitute.For<IServer>();
        server.Features.Returns(features);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IServer)).Returns(server);

        var options = new ArcOptions();
        options.EndpointGroups.Assign("internal", 5001);
        _validation = new EndpointGroupListenerValidation(services, Options.Create(options));
    }

    async Task Because() => _error = await Catch.Exception(() => _validation.StartedAsync(CancellationToken.None));

    [Fact] void should_refuse_to_run() => _error.ShouldBeOfExactType<NoPublicListenerForDefaultEndpointGroup>();
}

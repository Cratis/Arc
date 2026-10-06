// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_listener_has_scoped_authentication : given.a_host_with_scoped_authentication
{
    HttpListenerEndpointMapper _mapper;
    string? _problem;

    void Establish()
    {
        Build();
        _mapper = new HttpListenerEndpointMapper(NullLogger<HttpListenerEndpointMapper>.Instance, _services);
    }

    void Because() => _problem = ((IIntrospectionExposureGuard)_mapper).FindEnforcementProblem(null);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
    [Fact] void should_construct_a_scoped_handler() => _handlers.ShouldNotBeEmpty();
    [Fact] void should_dispose_scoped_handlers_asynchronously() => _handlers.TrueForAll(handler => handler.Disposed).ShouldBeTrue();

    void Destroy() => _mapper.Dispose();
}

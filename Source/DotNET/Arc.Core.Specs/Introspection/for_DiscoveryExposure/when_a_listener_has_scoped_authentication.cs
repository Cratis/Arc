// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_listener_has_scoped_authentication : Specification
{
    ServiceProvider _services;
    HttpListenerEndpointMapper _mapper;
    IAuthentication _authentication;
    string? _problem;

    void Establish()
    {
        _authentication = Substitute.For<IAuthentication, IDisposable>();
        _authentication.HasHandlers.Returns(true);
        var registrations = new ServiceCollection();
        registrations.AddScoped(_ => _authentication);
        _services = registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _mapper = new HttpListenerEndpointMapper(NullLogger<HttpListenerEndpointMapper>.Instance, _services);
    }

    void Because() => _problem = ((IIntrospectionExposureGuard)_mapper).FindEnforcementProblem(null);

    [Fact] void should_allow_authenticated_discovery() => _problem.ShouldBeNull();
    [Fact] void should_dispose_scoped_authentication() => ((IDisposable)_authentication).Received(1).Dispose();

    void Destroy()
    {
        _mapper.Dispose();
        _services.Dispose();
    }
}

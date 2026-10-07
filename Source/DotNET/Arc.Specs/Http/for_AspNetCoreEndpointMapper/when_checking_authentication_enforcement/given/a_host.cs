// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_authentication_enforcement.given;

public class a_host : Specification
{
    protected ServiceCollection _registrations;
    protected ServiceProvider _services;
    private protected IIntrospectionExposureGuard _guard;
    protected string? _problem;

    void Establish()
    {
        _registrations = new ServiceCollection();
        _registrations.AddLogging();
        _registrations.AddAuthentication("Discovery").AddCookie("Discovery");
    }

    protected void Build(bool supportsRegistrationInspection = true)
    {
        _services = _registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        IServiceProvider services = _services;
        if (!supportsRegistrationInspection)
        {
            services = Substitute.For<IServiceProvider>();
            services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(IServiceProviderIsService) ? null : _services.GetService(call.Arg<Type>()));
        }
        var endpoints = Substitute.For<IEndpointRouteBuilder>();
        endpoints.ServiceProvider.Returns(services);
        endpoints.DataSources.Returns(new List<EndpointDataSource>());
        _guard = new AspNetCoreEndpointMapper(endpoints);
    }

    void Destroy() => _services.Dispose();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.given;

public class a_host_with_scoped_authentication : Specification
{
    protected ServiceCollection _registrations;
    protected ServiceProvider _services;
    protected List<a_scoped_authentication_handler> _handlers;

    void Establish()
    {
        _handlers = [];
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IAuthenticationHandler>().Returns([typeof(a_scoped_authentication_handler)]);
        _registrations = new ServiceCollection();
        _registrations.AddLogging();
        _registrations.AddSingleton(types);
        _registrations.AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>));
        _registrations.AddTransient<IAuthentication, Authentication.Authentication>();
        _registrations.AddScoped(_ =>
        {
            var handler = new a_scoped_authentication_handler();
            _handlers.Add(handler);
            return handler;
        });
        _registrations.AddScoped<IAuthenticationHandler>(services => services.GetRequiredService<a_scoped_authentication_handler>());
    }

    protected void Build() => _services = _registrations.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    async Task Destroy() => await _services.DisposeAsync();
}

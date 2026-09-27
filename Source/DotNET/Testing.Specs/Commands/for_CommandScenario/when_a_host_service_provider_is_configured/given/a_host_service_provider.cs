// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_CommandScenario.when_a_host_service_provider_is_configured.given;

/// <summary>
/// Configures a host service provider as the process-wide provider, and restores whatever was there afterwards.
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class a_host_service_provider : Specification
{
    protected ServiceProvider _hostServiceProvider;
    IServiceProvider? _previousServiceProvider;

    void Establish()
    {
        _previousServiceProvider = CurrentServiceProviderOrNull();
        _hostServiceProvider = new ServiceCollection().BuildServiceProvider();
        Internals.ServiceProvider = _hostServiceProvider;
    }

    void Destroy()
    {
        Internals.ServiceProvider = _previousServiceProvider!;
        _hostServiceProvider.Dispose();
    }

    static IServiceProvider? CurrentServiceProviderOrNull()
    {
        try
        {
            return Internals.ServiceProvider;
        }
        catch (ServiceProviderNotConfigured)
        {
            return null;
        }
    }
}

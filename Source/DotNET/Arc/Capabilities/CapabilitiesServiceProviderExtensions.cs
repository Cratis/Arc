// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>Provides capability-host registration checks for optional transports.</summary>
public static class CapabilitiesServiceProviderExtensions
{
    /// <summary>Checks whether capability hosting was registered with WithCapabilities.</summary>
    /// <param name="services">The host's service provider.</param>
    /// <returns>Whether the capability endpoint gate is registered.</returns>
    public static bool HasCapabilities(this IServiceProvider services) => services.GetService<CapabilityHostingRegistration>() is not null;
}

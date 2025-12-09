// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Serialization;
using Cratis.Types;

namespace Cratis.Arc;

/// <summary>
/// Internal properties.
/// </summary>
internal static class Internals
{
    /// <summary>
    /// Gets the name of the meter used by the Arc.
    /// </summary>
    internal const string MeterName = "Cratis.Arc";
    static IServiceProvider? _serviceProvider;
    static ITypes? _types;
    static IDerivedTypes? _derivedTypes;
    static IEndpointMapper? _endpointMapper;

    /// <summary>
    /// Internal: The service provider.
    /// </summary>
    /// <exception cref="ServiceProviderNotConfigured">Thrown if the service provider has not been configured.</exception>
    internal static IServiceProvider ServiceProvider
    {
        get => _serviceProvider ?? throw new ServiceProviderNotConfigured();
        set => _serviceProvider = value;
    }

    /// <summary>
    /// Internal: The types.
    /// </summary>
    /// <exception cref="TypeDiscoverySystemNotConfigured">Thrown if the type discovery system has not been configured.</exception>
    internal static ITypes Types
    {
        get => _types ?? throw new TypeDiscoverySystemNotConfigured();
        set => _types = value;
    }

    /// <summary>
    /// Internal: The derived types.
    /// </summary>
    /// <exception cref="DerivedTypesSystemNotConfigured">Thrown if the derived types system has not been configured.</exception>
    internal static IDerivedTypes DerivedTypes
    {
        get => _derivedTypes ?? throw new DerivedTypesSystemNotConfigured();
        set => _derivedTypes = value;
    }

    /// <summary>
    /// Internal: The endpoint mapper.
    /// </summary>
    /// <exception cref="EndpointMapperNotConfigured">Thrown if the endpoint mapper has not been configured.</exception>
    internal static IEndpointMapper EndpointMapper
    {
        get => _endpointMapper ?? throw new EndpointMapperNotConfigured();
        set => _endpointMapper = value;
    }
}
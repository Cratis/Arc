// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Validates descriptor cardinality without creating scoped adapters at the root.</summary>
internal sealed class PatAdapterRegistration
{
    /// <summary>Checks explicit adapter registrations without resolving them.</summary>
    /// <param name="services">The installation service descriptors.</param>
    /// <returns>Whether every port has at most one unkeyed adapter.</returns>
    public static bool IsUnambiguous(IServiceCollection services) => new[] { typeof(IPatAuthority), typeof(IPatPolicy), typeof(IPatRecoveryEpoch) }
        .All(port => services.Count(descriptor => descriptor.ServiceType == port && !IsFallback(descriptor)) <= 1 &&
            !services.Any(descriptor => descriptor.ServiceType == port && descriptor.IsKeyedService));

    static bool IsFallback(ServiceDescriptor descriptor) => !descriptor.IsKeyedService &&
        (descriptor.ImplementationType == typeof(UnavailablePatAuthority) ||
         descriptor.ImplementationType == typeof(UnavailablePatPolicy) ||
         descriptor.ImplementationType == typeof(UnavailablePatRecoveryEpoch));
}

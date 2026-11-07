// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Validates descriptor cardinality without creating scoped adapters at the root.</summary>
internal static class PatAdapterRegistrationCheck
{
    /// <summary>Checks explicit adapter registrations without resolving them.</summary>
    /// <param name="services">The installation service descriptors.</param>
    /// <returns>A failure message, or null when every port has at most one unkeyed adapter.</returns>
    internal static string? Validate(IServiceCollection services)
    {
        foreach (var port in new[] { typeof(IPatAuthority), typeof(IPatPolicy), typeof(IPatRecoveryEpoch) })
        {
            var descriptors = services.Where(descriptor => descriptor.ServiceType == port).ToArray();
            if (descriptors.Any(descriptor => descriptor.IsKeyedService)) return "Ambiguous adapter registration.";
            if (descriptors.Count(descriptor => !IsFallback(descriptor)) > 1) return "Ambiguous adapter registration.";
        }
        return null;
    }

    static bool IsFallback(ServiceDescriptor descriptor) => descriptor.ImplementationType == typeof(UnavailablePatAuthority) ||
        descriptor.ImplementationType == typeof(UnavailablePatPolicy) || descriptor.ImplementationType == typeof(UnavailablePatRecoveryEpoch);
}

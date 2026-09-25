// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// Preserves the application's HTTP accessor registration while adding operation-local Arc identity scopes.
/// </summary>
internal static class OperationHttpContextAccessorRegistration
{
    /// <summary>
    /// Decorates the last registered HTTP accessor with an operation-local overlay.
    /// </summary>
    /// <param name="services">The application's services.</param>
    internal static void Add(IServiceCollection services)
    {
        var original = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor) && !descriptor.IsKeyedService);
        if (original is null)
        {
            services.AddHttpContextAccessor();
            original = services.Last(descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor) && !descriptor.IsKeyedService);
        }

        services.Remove(original);
        var innerKey = new object();
        services.Add(original.ImplementationInstance is { } instance
            ? ServiceDescriptor.KeyedSingleton(typeof(IHttpContextAccessor), innerKey, instance)
            : original.ImplementationFactory is { } factory
                ? ServiceDescriptor.DescribeKeyed(typeof(IHttpContextAccessor), innerKey, (provider, _) => factory(provider), original.Lifetime)
                : ServiceDescriptor.DescribeKeyed(typeof(IHttpContextAccessor), innerKey, original.ImplementationType!, original.Lifetime));
        services.Add(ServiceDescriptor.Describe(
            typeof(IHttpContextAccessor),
            provider => new OperationHttpContextAccessor(provider.GetRequiredKeyedService<IHttpContextAccessor>(innerKey)),
            original.Lifetime));
    }
}

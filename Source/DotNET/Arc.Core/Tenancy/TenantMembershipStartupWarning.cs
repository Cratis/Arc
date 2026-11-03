// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Warns once at startup when request-selected tenancy has no application membership check.
/// </summary>
/// <param name="services">The application's services.</param>
/// <param name="options">The Arc options.</param>
/// <param name="logger">The logger.</param>
[IgnoreConvention]
internal sealed class TenantMembershipStartupWarning(
    IServiceProvider services,
    IOptions<ArcOptions> options,
    ILogger<TenantMembershipStartupWarning> logger) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (options.Value.Tenancy.IngressValidatesTenant)
            {
                return;
            }

            var resolver = services.GetRequiredService<ITenantIdResolver>();
            if (resolver.GetType() != typeof(HeaderTenantIdResolver) &&
                resolver.GetType() != typeof(QueryTenantIdResolver) &&
                resolver.GetType() != typeof(SubdomainTenantIdResolver))
            {
                return;
            }

            var isService = services.GetService<IServiceProviderIsService>();
            if (isService is not null)
            {
                if (!isService.IsService(typeof(ITenantMembership)))
                {
                    logger.MembershipNotConfigured(resolver.GetType().Name);
                }
            }
            else
            {
                await using var scope = services.CreateAsyncScope();
                if (!scope.ServiceProvider.GetServices<ITenantMembership>().Any())
                {
                    logger.MembershipNotConfigured(resolver.GetType().Name);
                }
            }
        }
        catch
        {
            // A diagnostic must not prevent the host from starting.
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

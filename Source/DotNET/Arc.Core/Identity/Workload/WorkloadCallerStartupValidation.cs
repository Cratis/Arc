// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Refuses workload installation without an Arc capability boundary.</summary>
/// <param name="services">The configured host services.</param>
[IgnoreConvention]
internal sealed class WorkloadCallerStartupValidation(IServiceProvider services) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (services.GetService<WorkloadCapabilityHosting>() is null)
        {
            throw new InvalidVerifiedCallerInstallation("Workload callers require Arc capability hosting.");
        }

        // Resolve now: invalid or unreadable CA material must fail startup, not become another trust source.
        _ = services.GetRequiredService<IWorkloadTokenReviewClient>();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

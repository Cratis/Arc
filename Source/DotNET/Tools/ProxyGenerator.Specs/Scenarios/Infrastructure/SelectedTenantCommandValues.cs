// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Observes the tenant during pre-authorization context-value construction.
/// </summary>
/// <param name="services">The service provider constructing this context-values provider.</param>
/// <param name="observations">The test observations.</param>
public class SelectedTenantCommandValues(IServiceProvider services, TenantCommandObservations observations) : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        if (command is Scenarios.for_Commands.ModelBound.PolicyProtectedCommand)
        {
            observations.RecordValues(services.GetRequiredService<TenantBoundService>().Tenant.Value);
        }

        return new CommandContextValues();
    }
}

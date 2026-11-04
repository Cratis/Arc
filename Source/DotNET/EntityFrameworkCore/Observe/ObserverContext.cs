// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Represents the tenant and connection purpose an observer subscribed under, restored whenever it re-queries.
/// </summary>
/// <param name="tenant">The tenant captured at subscription, if any.</param>
/// <param name="purpose">The connection purpose captured at subscription.</param>
sealed class ObserverContext(TenantId? tenant, ConnectionPurpose purpose)
{
    /// <summary>
    /// Capture the context of the current execution flow.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> to find the tenant accessor in.</param>
    /// <returns>The captured <see cref="ObserverContext"/>.</returns>
    internal static ObserverContext Capture(IServiceProvider services) =>
        new(services.GetService<ITenantIdAccessor>()?.Current, ConnectionPurposeScope.Current);

    /// <summary>
    /// Begin a scope on the current execution flow with the captured context.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that restores the previous context when disposed.</returns>
    internal IDisposable Begin() => new Scopes(tenant, purpose);

    sealed class Scopes : IDisposable
    {
        readonly IDisposable _purpose;
        readonly IDisposable? _emission;
        readonly IDisposable? _connection;

        public Scopes(TenantId? tenant, ConnectionPurpose purpose)
        {
            _purpose = ConnectionPurposeScope.Begin(purpose);
            if (tenant is { } captured)
            {
                _emission = TenantIdAccessor.Independent.BeginEmission(captured);
                _connection = ConnectionTenantScope.Begin(captured);
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _emission?.Dispose();
            _purpose.Dispose();
        }
    }
}

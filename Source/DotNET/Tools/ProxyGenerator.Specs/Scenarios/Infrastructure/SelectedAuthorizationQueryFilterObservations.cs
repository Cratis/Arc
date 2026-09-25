// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Captures tenant binding at authorization-filter construction and invocation.
/// </summary>
public class SelectedAuthorizationQueryFilterObservations
{
    /// <summary>Gets the tenant at filter construction.</summary>
    public string? ConstructedTenant { get; private set; }

    /// <summary>Gets the scoped instance built in the authorization filter constructor.</summary>
    public Guid ConstructedServiceId { get; private set; }

    /// <summary>Gets the tenant at filter invocation.</summary>
    public string? ExecutedTenant { get; private set; }

    /// <summary>Clears observations before a request.</summary>
    public void Reset()
    {
        ConstructedTenant = null;
        ConstructedServiceId = Guid.Empty;
        ExecutedTenant = null;
    }

    /// <summary>Records the constructor's scoped tenant.</summary>
    /// <param name="tenant">The tenant.</param>
    /// <param name="serviceId">The scoped service instance.</param>
    public void RecordConstruction(string tenant, Guid serviceId)
    {
        ConstructedTenant = tenant;
        ConstructedServiceId = serviceId;
    }

    /// <summary>Records the invocation's scoped tenant.</summary>
    /// <param name="tenant">The tenant.</param>
    public void RecordExecution(string tenant) => ExecutedTenant = tenant;
}

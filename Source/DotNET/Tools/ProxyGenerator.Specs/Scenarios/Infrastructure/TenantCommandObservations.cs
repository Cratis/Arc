// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Records which tenant context providers and command scope callbacks use for a selected-scheme command.
/// </summary>
public class TenantCommandObservations
{
    /// <summary>
    /// Gets the tenant seen by command context-value providers before scope Begin.
    /// </summary>
    public string? ValuesTenant { get; private set; }

    /// <summary>
    /// Gets the tenant-bound service seen by execution-scope Begin.
    /// </summary>
    public string? BeginTenant { get; private set; }

    /// <summary>
    /// Gets the tenant-bound service seen by scope completion.
    /// </summary>
    public string? CompleteTenant { get; private set; }

    /// <summary>
    /// Clears observations before a serialized test request.
    /// </summary>
    public void Reset()
    {
        ValuesTenant = null;
        BeginTenant = null;
        CompleteTenant = null;
    }

    /// <summary>
    /// Records a context-value provider observation.
    /// </summary>
    /// <param name="tenant">The observed tenant.</param>
    public void RecordValues(string tenant) => ValuesTenant = tenant;

    /// <summary>
    /// Records a scope Begin observation.
    /// </summary>
    /// <param name="tenant">The observed tenant.</param>
    public void RecordBegin(string tenant) => BeginTenant = tenant;

    /// <summary>
    /// Records a scope Complete observation.
    /// </summary>
    /// <param name="tenant">The observed tenant.</param>
    public void RecordComplete(string tenant) => CompleteTenant = tenant;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Records the original request tenant before and after an Arc-owned selected-scheme execution.
/// </summary>
public class TenantFlowObservations
{
    readonly ConcurrentDictionary<string, TaskCompletionSource<(string Before, string After, Guid BeforeService, Guid AfterService)>> _flows = new();
    readonly ConcurrentDictionary<string, (string Tenant, Guid Service)> _beginnings = new();

    /// <summary>
    /// Registers an original request scope before the Arc endpoint runs.
    /// </summary>
    /// <param name="id">The test request identifier.</param>
    /// <param name="original">The original request's scoped tenant service.</param>
    public void Begin(string id, TenantBoundService original)
    {
        _flows.TryAdd(id, new(TaskCreationOptions.RunContinuationsAsynchronously));
        _beginnings[id] = (original.Tenant.Value, original.Id);
    }

    /// <summary>
    /// Completes an observation after the Arc endpoint has returned.
    /// </summary>
    /// <param name="id">The test request identifier.</param>
    /// <param name="original">The restored original request's scoped tenant service.</param>
    public void Complete(string id, TenantBoundService original)
    {
        if (_beginnings.TryRemove(id, out var before) && _flows.TryGetValue(id, out var signal))
        {
            signal.TrySetResult((before.Tenant, original.Tenant.Value, before.Service, original.Id));
        }
    }

    /// <summary>
    /// Waits for both pre- and post-execution identity observations.
    /// </summary>
    /// <param name="id">The test request identifier.</param>
    /// <returns>The original request's before/after tenant and scoped service identity.</returns>
    public async Task<(string Before, string After, Guid BeforeService, Guid AfterService)> Completed(string id)
    {
        var signal = _flows.GetOrAdd(id, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
        var result = await signal.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _flows.TryRemove(id, out _);
        return result;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability;

namespace Cratis.Arc.Availability;

/// <summary>
/// Holds what a developer has switched on or off while the application runs, so that availability can be explored live.
/// </summary>
/// <remarks>
/// The overrides take effect only in a local or test environment, as decided by <see cref="AvailabilityEnvironments"/>.
/// Anywhere else they are never accepted and never applied. They apply to the whole host, not to one caller.
/// </remarks>
/// <param name="environmentName">The name of the environment the host runs in.</param>
public class DevelopmentAvailability(string? environmentName)
{
    readonly Lock _lock = new();
    readonly List<Func<ValueTask>> _handlers = [];
    AvailabilityOverrides _overrides = AvailabilityOverrides.None;

    /// <summary>
    /// Gets a value indicating whether overrides are allowed in the environment of the host.
    /// </summary>
    public bool IsActive { get; } = AvailabilityEnvironments.AllowsGrantAll(environmentName);

    /// <summary>
    /// Gets the overrides in force. Always empty when overrides are not <see cref="IsActive"/>.
    /// </summary>
    public AvailabilityOverrides Current
    {
        get
        {
            lock (_lock)
            {
                return IsActive ? _overrides : AvailabilityOverrides.None;
            }
        }
    }

    /// <summary>
    /// Replaces the overrides and lets every open session decide again.
    /// </summary>
    /// <param name="overrides">The new overrides.</param>
    /// <returns>A task for continuation.</returns>
    /// <exception cref="InvalidOperationException">Overrides are not allowed in the environment of the host.</exception>
    /// <exception cref="AggregateException">One or more sessions failed to decide again; the overrides are applied regardless.</exception>
    public async Task Apply(AvailabilityOverrides overrides)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"Availability overrides are only allowed in a local or test environment, not in '{environmentName}'.");
        }

        Func<ValueTask>[] handlers;
        lock (_lock)
        {
            _overrides = AvailabilityOverrides.Normalize(overrides);
            handlers = [.. _handlers];
        }

        var failures = new List<Exception>();
        foreach (var handler in handlers)
        {
            try
            {
                await handler();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException(failures);
        }
    }

    /// <summary>
    /// Subscribes to changes of the overrides.
    /// </summary>
    /// <param name="handler">The handler to call each time the overrides are replaced.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the subscription.</returns>
    public IDisposable OnChanged(Func<ValueTask> handler)
    {
        lock (_lock)
        {
            _handlers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _handlers.Remove(handler);
            }
        });
    }

    sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

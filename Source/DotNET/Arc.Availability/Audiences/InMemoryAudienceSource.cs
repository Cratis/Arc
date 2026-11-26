// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// An in-memory <see cref="IAudienceSource"/>. Grants and withdrawals take effect immediately and notify subscribers.
/// </summary>
[IgnoreConvention]
public class InMemoryAudienceSource : IAudienceSource
{
    readonly Lock _lock = new();
    readonly HashSet<(AudienceLevel Level, string? Key, AudienceName Audience)> _grants = [];
    readonly List<Func<AudienceChange, ValueTask>> _handlers = [];

    /// <summary>
    /// Grants an audience at a level.
    /// </summary>
    /// <param name="level">The level to grant at.</param>
    /// <param name="key">The tenant or user; <see langword="null"/> for the system level.</param>
    /// <param name="audience">The audience to grant.</param>
    /// <returns>A task completing once subscribers are notified.</returns>
    public Task Grant(AudienceLevel level, string? key, AudienceName audience) => Change(level, key, audience, add: true);

    /// <summary>
    /// Withdraws an audience at a level. Only that level is affected.
    /// </summary>
    /// <param name="level">The level to withdraw at.</param>
    /// <param name="key">The tenant or user; <see langword="null"/> for the system level.</param>
    /// <param name="audience">The audience to withdraw.</param>
    /// <returns>A task completing once subscribers are notified.</returns>
    public Task Withdraw(AudienceLevel level, string? key, AudienceName audience) => Change(level, key, audience, add: false);

    /// <inheritdoc/>
    public ValueTask<IReadOnlyCollection<AudienceGrant>> GetGrants(AudienceSubject subject, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyCollection<AudienceGrant> result = [.. _grants
                .Where(_ => IsFor(subject, _.Level, _.Key))
                .Select(_ => new AudienceGrant(_.Audience, _.Level))];
            return ValueTask.FromResult(result);
        }
    }

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<AudienceChange, ValueTask> handler)
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

    static bool IsFor(AudienceSubject subject, AudienceLevel level, string? key) => level switch
    {
        AudienceLevel.Tenant => subject.Tenant is not null && subject.Tenant == key,
        AudienceLevel.User => subject.User is not null && subject.User == key,
        AudienceLevel.System => subject.IsSystem,
        _ => false
    };

    async Task Change(AudienceLevel level, string? key, AudienceName audience, bool add)
    {
        Func<AudienceChange, ValueTask>[] handlers;
        lock (_lock)
        {
            var effectiveKey = level == AudienceLevel.System ? null : key;
            _ = add ? _grants.Add((level, effectiveKey, audience)) : _grants.Remove((level, effectiveKey, audience));
            handlers = [.. _handlers];
        }

        var change = new AudienceChange(level, level == AudienceLevel.System ? null : key);
        foreach (var handler in handlers)
        {
            await handler(change);
        }
    }

    sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

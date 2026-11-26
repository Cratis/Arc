// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// An in-memory <see cref="ISuspensionSource"/>.
/// </summary>
[IgnoreConvention]
public class InMemorySuspensionSource : ISuspensionSource
{
    readonly Lock _lock = new();
    readonly List<(string? Key, PartSuspensionDecision Decision)> _decisions = [];
    readonly List<Func<SuspensionChange, ValueTask>> _handlers = [];

    /// <summary>
    /// Suspends a part.
    /// </summary>
    /// <param name="level">The <see cref="AudienceLevel"/> to suspend at.</param>
    /// <param name="key">The tenant or user; ignored for the system level.</param>
    /// <param name="path">The module, feature, slice or part path, see <see cref="PartPaths"/>.</param>
    /// <returns>An awaitable task.</returns>
    public Task Suspend(AudienceLevel level, string? key, string path) => Decide(level, key, path, true);

    /// <summary>
    /// Resumes a part.
    /// </summary>
    /// <param name="level">The <see cref="AudienceLevel"/> to resume at.</param>
    /// <param name="key">The tenant or user; ignored for the system level.</param>
    /// <param name="path">The module, feature, slice or part path, see <see cref="PartPaths"/>.</param>
    /// <returns>An awaitable task.</returns>
    public Task Resume(AudienceLevel level, string? key, string path) => Decide(level, key, path, false);

    /// <inheritdoc/>
    public ValueTask<IReadOnlyCollection<PartSuspensionDecision>> GetDecisions(AudienceSubject subject, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyCollection<PartSuspensionDecision> result = [.. _decisions.Where(_ => IsFor(subject, _.Decision.Level, _.Key)).Select(_ => _.Decision)];
            return ValueTask.FromResult(result);
        }
    }

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<SuspensionChange, ValueTask> handler)
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

    async Task Decide(AudienceLevel level, string? key, string path, bool suspended)
    {
        Func<SuspensionChange, ValueTask>[] handlers;
        var effectiveKey = level == AudienceLevel.System ? null : key;
        lock (_lock)
        {
            _decisions.Add((effectiveKey, new PartSuspensionDecision(path, level, suspended, DateTimeOffset.UtcNow)));
            handlers = [.. _handlers];
        }

        var change = new SuspensionChange(level, effectiveKey);
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

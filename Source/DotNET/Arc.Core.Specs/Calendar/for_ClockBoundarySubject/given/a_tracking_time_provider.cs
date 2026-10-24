// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject.given;

/// <summary>
/// A <see cref="FakeTimeProvider"/> that counts the timers it created and disposed, and whose clock reading can be skewed
/// from the time its timers fire on.
/// </summary>
/// <param name="start">The start of the clock.</param>
public class a_tracking_time_provider(DateTimeOffset start) : FakeTimeProvider(start)
{
    int _created;
    int _disposed;

    /// <summary>
    /// Gets the number of timers created.
    /// </summary>
    public int TimersCreated => Volatile.Read(ref _created);

    /// <summary>
    /// Gets the number of timers disposed.
    /// </summary>
    public int TimersDisposed => Volatile.Read(ref _disposed);

    /// <summary>
    /// Gets or sets the amount added to every clock reading.
    /// </summary>
    public TimeSpan Skew { get; set; }

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + Skew;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Interlocked.Increment(ref _created);
        return new TrackedTimer(base.CreateTimer(callback, state, dueTime, period), () => Interlocked.Increment(ref _disposed));
    }

    sealed class TrackedTimer(ITimer inner, Action onDispose) : ITimer
    {
        int _isDisposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                onDispose();
            }

            inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                onDispose();
            }

            return inner.DisposeAsync();
        }
    }
}

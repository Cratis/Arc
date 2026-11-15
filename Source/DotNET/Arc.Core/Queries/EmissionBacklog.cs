// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Bounds how many emissions may be pending for one observable query subscription: the one being processed plus
/// the ones waiting behind it.
/// </summary>
/// <param name="capacity">The maximum number of pending emissions.</param>
internal sealed class EmissionBacklog(int capacity)
{
    int _pending;
    int _overflowed;

    /// <summary>
    /// Gets the number of emissions currently holding a slot.
    /// </summary>
    internal int Pending => Volatile.Read(ref _pending);

    /// <summary>
    /// Takes a slot for an emission.
    /// </summary>
    /// <returns>True when a slot was taken and must be released; false when the backlog is full and no slot is held.</returns>
    internal bool TryAdmit()
    {
        if (Interlocked.Increment(ref _pending) <= capacity)
        {
            return true;
        }

        Interlocked.Decrement(ref _pending);
        return false;
    }

    /// <summary>
    /// Returns a slot taken by <see cref="TryAdmit"/>.
    /// </summary>
    internal void Release() => Interlocked.Decrement(ref _pending);

    /// <summary>
    /// Accepts an emission delivered directly by a subject: registers it with the drain and takes a slot. When the
    /// backlog is full the emission is dropped and <paramref name="onOverflow"/> runs once for the subscription.
    /// </summary>
    /// <param name="drain">The <see cref="DirectObservableEmissionDrain"/> tracking the connection's callbacks.</param>
    /// <param name="onOverflow">Ends the connection; invoked for the first overflow only.</param>
    /// <returns>True when the emission was accepted and must be left through <see cref="Leave"/>.</returns>
    internal bool TryEnter(DirectObservableEmissionDrain drain, Action onOverflow)
    {
        if (!drain.TryEnter())
        {
            return false;
        }

        if (TryAdmit())
        {
            return true;
        }

        drain.Exit();
        if (TryMarkOverflowed())
        {
            onOverflow();
        }

        return false;
    }

    /// <summary>
    /// Leaves an emission accepted by <see cref="TryEnter"/>.
    /// </summary>
    /// <param name="drain">The <see cref="DirectObservableEmissionDrain"/> the emission was registered with.</param>
    internal void Leave(DirectObservableEmissionDrain drain)
    {
        Release();
        drain.Exit();
    }

    /// <summary>
    /// Records that the backlog overflowed.
    /// </summary>
    /// <returns>True for the first caller only, so the overflow is handled exactly once.</returns>
    internal bool TryMarkOverflowed() => Interlocked.CompareExchange(ref _overflowed, 1, 0) == 0;
}

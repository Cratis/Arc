// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Keeps Chronicle read regions from crossing a command boundary.
/// </summary>
/// <remarks>
/// Chronicle's read regions flow with the asynchronous context, so a region opened by a command body flows into a
/// command it executes. Chronicle suspends inherited regions for its own reducer callbacks, but does not expose that
/// suspension publicly, so Arc binds to it once. If a Chronicle version no longer has it, beginning a nested command
/// that enables read protection fails rather than letting it run with its caller's allowances.
/// </remarks>
internal static class ReadRegions
{
    const string RegionTypeName = "Cratis.Chronicle.Transactions.ReadProtectionReadRegion, Cratis.Chronicle";
    const string SuspendMethodName = "Suspend";

    static readonly Func<IDisposable>? _suspend = BindSuspend();

    /// <summary>
    /// Suspends every read region inherited by this asynchronous flow until the returned lease is disposed.
    /// </summary>
    /// <returns>The suspension lease.</returns>
    /// <exception cref="ReadRegionsCannotBeSuspended">The Chronicle client does not support suspending read regions.</exception>
    internal static IDisposable SuspendInherited() => (_suspend ?? throw new ReadRegionsCannotBeSuspended())();

    static Func<IDisposable>? BindSuspend()
    {
        var method = Type.GetType(RegionTypeName)?.GetMethod(SuspendMethodName, BindingFlags.Static | BindingFlags.NonPublic, Type.EmptyTypes);
        return method is not null && typeof(IDisposable).IsAssignableFrom(method.ReturnType)
            ? method.CreateDelegate<Func<IDisposable>>()
            : null;
    }
}

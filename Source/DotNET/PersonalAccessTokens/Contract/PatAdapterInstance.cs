// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>An isolated adapter under test with mandatory real-store failure controls.</summary>
/// <param name="Authority">The real persistence adapter.</param>
/// <param name="RecoveryEpoch">The real external recovery authority.</param>
/// <param name="Policy">The current product policy adapter.</param>
public sealed record PatAdapterInstance(IPatAuthority Authority, IPatRecoveryEpoch RecoveryEpoch, IPatPolicy Policy) : IAsyncDisposable
{
    /// <summary>Gets the external recovery-epoch advancement hook.</summary>
    public Func<Task>? AdvanceRecoveryEpoch { get; init; }

    /// <summary>Gets the restore hook, including an in-place restore and external recovery fencing.</summary>
    public Func<Task>? Restore { get; init; }

    /// <summary>Gets the hook that enables or clears outages of all three authorities.</summary>
    public Func<bool, Task>? SetOutage { get; init; }

    /// <summary>Gets the hook that removes external recovery evidence.</summary>
    public Func<Task>? RemoveRecoveryEvidence { get; init; }

    /// <summary>Gets the hook that denies all current policy decisions.</summary>
    public Func<Task>? DenyEverything { get; init; }

    /// <summary>Gets the hook that permits current policy with the supplied lifetime ceiling.</summary>
    public Func<TimeSpan, Task>? Permit { get; init; }

    /// <summary>Gets optional asynchronous cleanup of adapter-owned test resources.</summary>
    public Func<ValueTask>? Dispose { get; init; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => Dispose?.Invoke() ?? ValueTask.CompletedTask;

    /// <summary>Checks that every required failure control is available.</summary>
    /// <returns>Whether all mandatory hooks are supplied.</returns>
    internal bool HasRequiredHooks() => AdvanceRecoveryEpoch is not null && Restore is not null && SetOutage is not null &&
        RemoveRecoveryEvidence is not null && DenyEverything is not null && Permit is not null;
}

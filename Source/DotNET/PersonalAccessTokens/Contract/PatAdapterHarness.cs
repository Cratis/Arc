// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>
/// Adapter-owned test controls. Every hook is required; absent hooks fail rather than skip.
/// Each Create call must provide an isolated installation with the documented synthetic policy fixtures enabled.
/// Hooks must use the real persistence and external recovery authority, not replace them with test doubles.
/// </summary>
public sealed class PatAdapterHarness
{
    /// <summary>Gets the isolated adapter factory.</summary>
    public Func<CancellationToken, Task<PatAdapterInstance>>? Create { get; init; }

    /// <summary>Gets the query proving an issuance intent committed for a credential.</summary>
    public Func<PatAdapterInstance, CredentialId, CancellationToken, Task<bool>>? HasIssuedIntent { get; init; }

    /// <summary>Gets the hook that fails the next issuance transaction before commit.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? FailNextCommit { get; init; }

    /// <summary>Gets a second adapter process connected to the same authority.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task<PatAdapterInstance>>? SecondProcess { get; init; }

    /// <summary>Gets the store snapshot hook, excluding external recovery evidence.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? Snapshot { get; init; }

    /// <summary>Gets the store restore hook, preserving external recovery evidence.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? Restore { get; init; }

    /// <summary>Gets the external recovery-epoch advancement hook.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? AdvanceRecoveryEpoch { get; init; }

    /// <summary>Gets the hook that removes external recovery evidence.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? RemoveRecoveryEvidence { get; init; }

    /// <summary>Gets the hook that blocks authority reads.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? BlockAuthority { get; init; }

    /// <summary>Gets the hook that blocks recovery reads.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? BlockRecovery { get; init; }

    /// <summary>Gets the hook that makes current policy unavailable.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? BlockPolicy { get; init; }

    /// <summary>Gets the hook that disables the synthetic person.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? DisablePerson { get; init; }

    /// <summary>Gets the hook that removes synthetic membership.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? RemoveMembership { get; init; }

    /// <summary>Gets the hook that reduces the synthetic lifetime ceiling below one hour.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? ReduceLifetime { get; init; }

    /// <summary>Gets the hook that removes the synthetic read scope.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? ReduceScopes { get; init; }

    /// <summary>Gets the hook that expires synthetic upstream SSO.</summary>
    public Func<PatAdapterInstance, CancellationToken, Task>? ExpireSso { get; init; }
}

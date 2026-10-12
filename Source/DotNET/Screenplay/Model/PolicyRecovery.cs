// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Retains recovery completeness without changing the policy's public contract or value equality.
/// </summary>
/// <param name="Kind">How completely the policy was recovered.</param>
internal sealed record PolicyRecovery(PolicyRecoveryKind Kind)
{
    static readonly ConditionalWeakTable<PolicyModel, PolicyRecovery> _evidence = new();

    /// <summary>
    /// Records analysis completeness for one policy.
    /// </summary>
    /// <param name="policy">The recovered policy.</param>
    /// <param name="incomplete">Whether analysis reported unsupported requirements.</param>
    internal static void Register(PolicyModel policy, bool incomplete)
    {
        var kind = policy.Requirement is null ? PolicyRecoveryKind.Unrecovered :
            incomplete ? PolicyRecoveryKind.Partial : PolicyRecoveryKind.Exact;
        _evidence.Add(policy, new(kind));
    }

    /// <summary>
    /// Determines whether analysis proved every policy requirement.
    /// </summary>
    /// <param name="policy">The recovered policy.</param>
    /// <returns>Whether recovery was exact.</returns>
    internal static bool IsExact(PolicyModel policy) =>
        _evidence.TryGetValue(policy, out var recovery) && recovery.Kind == PolicyRecoveryKind.Exact;
}

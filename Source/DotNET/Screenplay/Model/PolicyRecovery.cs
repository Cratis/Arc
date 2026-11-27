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
    static readonly ConditionalWeakTable<PolicyModel, ScreenplayDiagnostic> _refusals = new();
    int _incomplete;

    /// <summary>
    /// Records analysis completeness for one policy.
    /// </summary>
    /// <param name="policy">The recovered policy.</param>
    /// <param name="incomplete">Whether analysis reported unsupported requirements.</param>
    internal static void Register(PolicyModel policy, bool incomplete)
    {
        var kind = policy.Requirement is null ? PolicyRecoveryKind.Unrecovered :
            incomplete ? PolicyRecoveryKind.Partial : PolicyRecoveryKind.Exact;
        var recovery = _evidence.GetValue(policy, _ => new(kind));
        if (incomplete)
        {
            Interlocked.Exchange(ref recovery._incomplete, 1);
        }
    }

    /// <summary>
    /// Determines whether analysis proved every policy requirement.
    /// </summary>
    /// <param name="policy">The recovered policy.</param>
    /// <returns>Whether recovery was exact.</returns>
    internal static bool IsExact(PolicyModel policy) =>
        !_refusals.TryGetValue(policy, out _) && _evidence.TryGetValue(policy, out var recovery) &&
        recovery.Kind == PolicyRecoveryKind.Exact && Volatile.Read(ref recovery._incomplete) == 0;

    /// <summary>
    /// Associates immutable registration refusal evidence without changing the policy or its requirement.
    /// </summary>
    /// <param name="policy">The recovered declaration.</param>
    /// <param name="reason">Why its anonymous registration cannot be represented faithfully.</param>
    /// <param name="location">Where the registration ambiguity is reported.</param>
    /// <returns>The stable refusal diagnostic associated with this policy.</returns>
    internal static ScreenplayDiagnostic Refuse(PolicyModel policy, string reason, string? location) =>
        _refusals.GetValue(policy, _ => new(
            ScreenplayDiagnosticSeverity.Error,
            ScreenplayDiagnosticCodes.AmbiguousAnonymousPolicyRegistration,
            $"The policy '{policy.Name}' has ambiguous anonymous registration: {reason}. No executable document was emitted",
            location));

    /// <summary>
    /// Gets the immutable registration refusal carried by a declaration through scoped model copies.
    /// </summary>
    /// <param name="policy">The policy to inspect.</param>
    /// <returns>The refusal diagnostic, or null when registration did not record a refusal.</returns>
    internal static ScreenplayDiagnostic? RefusalFor(PolicyModel policy) =>
        _refusals.TryGetValue(policy, out var refusal) ? refusal : null;
}

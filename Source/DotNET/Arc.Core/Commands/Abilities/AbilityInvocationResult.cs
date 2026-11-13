// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// What running an ability produced.
/// </summary>
/// <param name="Outcome">The outcome name: <see cref="AbilityOutcomes.Returned"/>, <see cref="AbilityOutcomes.Failed"/> or a declared outcome.</param>
/// <param name="Returns">The ability's returned value, if any.</param>
/// <param name="Reason">Why the ability failed, when it did.</param>
public record AbilityInvocationResult(string Outcome, object? Returns = default, string? Reason = default)
{
    /// <summary>Creates a result for an ability that returned.</summary>
    /// <param name="returns">The returned value, if any.</param>
    /// <returns>The result.</returns>
    public static AbilityInvocationResult ReturnedWith(object? returns = default) => new(AbilityOutcomes.Returned, returns);

    /// <summary>Creates a result for an ability that failed.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The result.</returns>
    public static AbilityInvocationResult FailedWith(string reason) => new(AbilityOutcomes.Failed, default, reason);
}

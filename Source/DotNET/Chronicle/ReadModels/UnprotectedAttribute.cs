// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Marks a command as using advisory, unguarded reads. On plain read-model parameters this is only an
/// acknowledgement for tooling; parameter-level runtime opt-out is not supported.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Method)]
public sealed class UnprotectedAttribute : Attribute, IUnprotectedDecisionAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnprotectedAttribute"/> class without a reason.
    /// </summary>
    public UnprotectedAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnprotectedAttribute"/> class with an advisory read reason.
    /// </summary>
    /// <param name="reason">Why the read is intentionally advisory.</param>
    public UnprotectedAttribute(string reason) => Reason = reason;

    /// <summary>
    /// Gets the optional reason the read is advisory.
    /// </summary>
    public string? Reason { get; }
}

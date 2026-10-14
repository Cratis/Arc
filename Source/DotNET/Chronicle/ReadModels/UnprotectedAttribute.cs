// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Declares advisory, unguarded reads. On a command this disables read protection; on a read-model type
/// or parameter it marks that read as advisory. On a method it applies only to reads in the method body,
/// not to injected parameters. An optional reason explains why protection is not required.
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

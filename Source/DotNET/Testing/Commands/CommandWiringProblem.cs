// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>A wiring failure, without command values or service exception messages.</summary>
/// <param name="CommandType">The command, or null for a provider-level failure.</param>
/// <param name="Member">The member being checked.</param>
/// <param name="Parameter">The parameter declaration.</param>
/// <param name="Category">The stable failure category.</param>
/// <param name="Guidance">The corrective action.</param>
/// <param name="Cause">The original failure, when available.</param>
public sealed record CommandWiringProblem(Type? CommandType, string Member, string Parameter, string Category, string Guidance, Exception? Cause = null)
{
    /// <inheritdoc/>
    public override string ToString() => $"{CommandType?.FullName ?? "<provider>"} | {Member}{Parameter} | {Category} | {Guidance}";
}

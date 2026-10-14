// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines the freshness policy for a generated value used as an append target.
/// </summary>
public enum GeneratedValueFreshness
{
    /// <summary>
    /// Requires the target event source to have no events before this invocation appends.
    /// </summary>
    Guarded = 0,

    /// <summary>
    /// Leaves concurrency protection to the command's own policy.
    /// </summary>
    Unguarded = 1
}

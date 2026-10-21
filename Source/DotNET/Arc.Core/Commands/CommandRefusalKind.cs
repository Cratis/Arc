// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Identifies a business refusal of a command, distinct from a transient or infrastructure failure.
/// </summary>
public enum CommandRefusalKind
{
    /// <summary>
    /// An authored validation rule refused the command.
    /// </summary>
    Validation = 0,

    /// <summary>
    /// An append constraint refused the command.
    /// </summary>
    Constraint = 1,

    /// <summary>
    /// Authorization denied the command.
    /// </summary>
    Authorization = 2
}

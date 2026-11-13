// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Identifies which phase of a command operation is running.
/// </summary>
public enum CommandOperationPhase
{
    /// <summary>The operation's Execute method, which runs before the command commits.</summary>
    Execute = 0,

    /// <summary>The operation's Compensate method, which runs when the command does not commit.</summary>
    Compensate = 1
}

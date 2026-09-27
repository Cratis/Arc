// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>The command's declared decision read profile.</summary>
public enum CommandDecisionMode
{
    /// <summary>No protected decisions; existing commands and validators remain unchanged.</summary>
    Legacy = 0,

    /// <summary>Protected decision reads and restricted per-invocation validators.</summary>
    Protected = 1,

    /// <summary>Explicitly advisory and unguarded decisions.</summary>
    Unprotected = 2
}

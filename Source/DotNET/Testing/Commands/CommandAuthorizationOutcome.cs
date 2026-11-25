// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>The outcome of a standalone command authorization evaluation.</summary>
public enum CommandAuthorizationOutcome
{
    /// <summary>The runtime authorized the command.</summary>
    Authorized = 0,

    /// <summary>The runtime denied the command.</summary>
    Denied = 1,

    /// <summary>The standalone adapter cannot evaluate this configuration.</summary>
    Unsupported = 2
}

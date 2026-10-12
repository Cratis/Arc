// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Distinguishes exact, partial, and wholly unrecovered policy requirements.
/// </summary>
internal enum PolicyRecoveryKind
{
    /// <summary>
    /// Every requirement was recovered.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Some requirements were recovered and some were omitted.
    /// </summary>
    Partial = 1,

    /// <summary>
    /// No requirement was recovered, including an unregistered policy.
    /// </summary>
    Unrecovered = 2
}

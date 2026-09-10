// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The phase in which the original command failure was observed.
/// </summary>
public enum CommandOperationFailureSource
{
    /// <summary>
    /// Planning or dependency preflight failed.
    /// </summary>
    Planning = 0,

    /// <summary>
    /// A control value or response handler rejected the command.
    /// </summary>
    ResponseHandling = 1,

    /// <summary>
    /// An operation threw.
    /// </summary>
    Execution = 2,

    /// <summary>
    /// Forward execution was canceled.
    /// </summary>
    Cancellation = 3,

    /// <summary>
    /// An execution scope failed or rejected completion.
    /// </summary>
    ScopeCompletion = 4
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Integration-reported facts about the coordinated business commit, independent of command success.
/// </summary>
public enum CommandCommitDisposition
{
    /// <summary>
    /// No coordinated commit participant exists.
    /// </summary>
    NoCommit = 0,

    /// <summary>
    /// All coordinated changes are known not committed.
    /// </summary>
    NotCommitted = 1,

    /// <summary>
    /// The coordinated business boundary committed.
    /// </summary>
    Committed = 2,

    /// <summary>
    /// Commitment cannot be established safely.
    /// </summary>
    Unknown = 3,

    /// <summary>
    /// Some changes committed and others did not.
    /// </summary>
    Mixed = 4
}

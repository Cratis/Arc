// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Observed compensation outcome for one started invocation.
/// </summary>
public enum CommandOperationCompensation
{
    /// <summary>
    /// Compensation was not required.
    /// </summary>
    NotNeeded = 0,

    /// <summary>
    /// The compensator returned.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The compensator threw.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// No compensator was declared.
    /// </summary>
    NotAvailable = 3,

    /// <summary>
    /// The cooperative recovery budget expired before entry.
    /// </summary>
    BudgetExpired = 4,

    /// <summary>
    /// Commitment facts prohibit automatic reversal.
    /// </summary>
    Suppressed = 5
}

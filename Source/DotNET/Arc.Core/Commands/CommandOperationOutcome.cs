// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Immutable server-side observation of a started operation. Never included in the transport recovery summary.
/// </summary>
/// <param name="InvocationIndex">Zero-based declaration index.</param>
/// <param name="OperationType">The declaration type, without its business payload.</param>
/// <param name="ExecutionCompleted">Whether Execute returned successfully.</param>
/// <param name="Compensation">The observed compensation outcome.</param>
/// <param name="CompensationFailure">Optional server-only failure message.</param>
public sealed record CommandOperationOutcome(
    int InvocationIndex,
    Type OperationType,
    bool ExecutionCompleted,
    CommandOperationCompensation Compensation,
    string? CompensationFailure = null);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Exception thrown when a validator reads the operation's tenant or caller outside Arc's command or query pipeline.
/// </summary>
/// <remarks>
/// Reading fails closed: the operation is rejected as unauthorized, and no allowed validation severity can hide it.
/// </remarks>
public sealed class OperationSnapshotUnavailable() : Exception("The operation's tenant and caller are only available while Arc's command or query pipeline validates the operation.")
{
    /// <summary>
    /// Checks whether an exception or one of its nested causes is an unavailable operation snapshot.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>True if an unavailable operation snapshot is found within sixteen nesting levels.</returns>
    internal static bool IsCauseOf(Exception? exception) => IsCauseOf(exception, 16);

    static bool IsCauseOf(Exception? exception, int remaining) => remaining > 0 && exception switch
    {
        OperationSnapshotUnavailable => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(inner => IsCauseOf(inner, remaining - 1)),
        { InnerException: { } inner } => IsCauseOf(inner, remaining - 1),
        _ => false
    };
}

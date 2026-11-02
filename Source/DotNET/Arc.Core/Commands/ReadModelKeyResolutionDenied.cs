// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>A caller-derived key could not be resolved. Contains no caller or provider exception data.</summary>
/// <param name="reason">The safe refusal reason.</param>
public sealed class ReadModelKeyResolutionDenied(ReadModelKeyRefusal reason) : Exception("Read-model key resolution was denied.")
{
    /// <summary>Gets the safe refusal reason.</summary>
    public ReadModelKeyRefusal Reason { get; } = reason;

    /// <summary>Checks for a denial within sixteen nesting levels.</summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>Whether a denial is a cause.</returns>
    public static bool IsCauseOf(Exception? exception) => IsCauseOf(exception, 16);

    static bool IsCauseOf(Exception? exception, int remaining) => remaining > 0 && exception switch
    {
        ReadModelKeyResolutionDenied => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(inner => IsCauseOf(inner, remaining - 1)),
        { InnerException: { } inner } => IsCauseOf(inner, remaining - 1),
        _ => false
    };
}

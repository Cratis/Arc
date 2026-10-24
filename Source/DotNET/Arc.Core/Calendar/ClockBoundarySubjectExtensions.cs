// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Extension methods for re-evaluating a live answer at clock boundaries.
/// </summary>
public static class ClockBoundarySubjectExtensions
{
    /// <summary>
    /// Re-evaluates the latest candidates whenever they change and whenever a clock boundary passes, emitting only when
    /// the answer changes.
    /// </summary>
    /// <typeparam name="TSource">The type of the candidates.</typeparam>
    /// <typeparam name="TResult">The type of the answer.</typeparam>
    /// <param name="candidates">The subject of candidates, observed without any clock-dependent filter.</param>
    /// <param name="context">The <see cref="ClockBoundaryContext"/> of the subscription.</param>
    /// <param name="evaluate">Evaluates the latest candidates at a <see cref="ClockReading"/> and states the next boundary.</param>
    /// <returns>A subject that streams the answers. It connects on the first subscription and releases its timer and its
    /// subscription to the candidates when the last subscription ends, the context is cancelled, or the stream errors or completes.</returns>
    /// <remarks>
    /// An evaluation whose result equals the last emitted result is not emitted; the first is always emitted. Calling
    /// <see cref="IObserver{T}.OnNext"/> on the returned subject is not supported, while <see cref="IObserver{T}.OnError"/> and
    /// <see cref="IObserver{T}.OnCompleted"/> end it. `evaluate` runs one at a time, inside the tenant of the context.
    /// </remarks>
    public static ISubject<TResult> ReevaluateAtBoundaries<TSource, TResult>(
        this ISubject<TSource> candidates,
        ClockBoundaryContext context,
        Func<TSource, ClockReading, ClockEvaluation<TResult>> evaluate) =>
        new ClockBoundarySubject<TSource, TResult>(candidates, context, evaluate);
}

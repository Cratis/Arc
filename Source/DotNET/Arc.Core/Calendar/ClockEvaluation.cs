// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents the result of evaluating a clock-dependent answer.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="Result">The result at the evaluated instant.</param>
/// <param name="NextBoundary">The earliest instant at which the result may change because of the clock, or <see langword="null"/> when it cannot. It must be after the instant of the reading the evaluation ran at, otherwise the subscription ends with a <see cref="ClockBoundaryIsNotInTheFuture"/>.</param>
public record ClockEvaluation<TResult>(TResult Result, DateTimeOffset? NextBoundary);

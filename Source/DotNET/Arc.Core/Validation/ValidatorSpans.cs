// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Arc.Validation;

/// <summary>
/// Decides which validator invocations in one model graph validation get a span, and starts them.
/// </summary>
/// <remarks>
/// A validator runs once for every instance of the type it validates, so a command carrying a collection would
/// otherwise raise a span per item. Only the first invocation of each validator type gets one, and one validation
/// raises at most <see cref="MaxSpans"/>, so what a trace holds is bounded by the model, not by the payload.
/// A graph is walked one instance at a time, so this is not shared between threads.
/// </remarks>
/// <param name="source">The <see cref="ActivitySource"/> to start the spans on.</param>
internal sealed class ValidatorSpans(ActivitySource source)
{
    /// <summary>
    /// The largest number of validator spans one validation raises.
    /// </summary>
    internal const int MaxSpans = 16;

    readonly HashSet<Type> _traced = [];

    /// <summary>
    /// Gets the validator spans for one validation, when anything listens to them.
    /// </summary>
    /// <param name="activitySource">The <see cref="IActivitySource{T}"/> to trace on, if any.</param>
    /// <returns>The <see cref="ValidatorSpans"/>, or <see langword="null"/> when there is nothing to trace to.</returns>
    internal static ValidatorSpans? For(IActivitySource<ModelGraphValidator>? activitySource) =>
        activitySource?.ActualSource is { } source && source.HasListeners() ? new(source) : null;

    /// <summary>
    /// Starts a span for invoking a validator, unless the validator type already has one or the limit is reached.
    /// </summary>
    /// <param name="validatorType">The type of the validator.</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> when the invocation gets none.</returns>
    internal Activity? Start(Type validatorType)
    {
        if (_traced.Count >= MaxSpans || !_traced.Add(validatorType))
        {
            return null;
        }

        var activity = source.StartActivity(WellKnownTelemetryNames.ValidatorInvokeSpan, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.ValidatorType, validatorType.FullName ?? validatorType.Name);
        }

        return activity;
    }
}

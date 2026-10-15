// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Best-effort observations that cannot change command outcomes.
/// </summary>
internal static class ReadProtectionTelemetry
{
    static readonly Meter _meter = new("Cratis.Arc.ReadProtection");
    static readonly Counter<long> _events = _meter.CreateCounter<long>("cratis.arc.read_protection.events");

    static long _instrumentationFailures;

    /// <summary>
    /// Records a hypothetical read-protection outcome without affecting the invocation.
    /// </summary>
    /// <param name="command">The command being observed.</param>
    /// <param name="category">The observation category.</param>
    /// <param name="model">The read model, when known.</param>
    /// <param name="reason">The observation's reason, when applicable.</param>
    /// <param name="logger">An optional structured log sink.</param>
    internal static void Record(Type command, string category, Type? model = null, string? reason = null, ILogger<ReadProtectionMeasurement>? logger = null)
    {
        try
        {
            logger ??= ReadProtectionMeasurement.Current?.Logger;
            logger?.Hypothesis(command.FullName, category, model?.FullName, reason);
            _events.Add(1, new KeyValuePair<string, object?>("command", command.FullName), new("category", category), new("model", model?.FullName), new("reason", reason));
            Activity.Current?.AddEvent(new ActivityEvent("read-protection.plan", tags: new ActivityTagsCollection
            {
                { "command", command.FullName },
                { "category", category },
                { "model", model?.FullName },
                { "reason", reason }
            }));
        }
        catch (Exception failure)
        {
            Interlocked.Increment(ref _instrumentationFailures);

            // Give independent sinks one bounded chance to report unavailable; never recurse through Record.
            try
            {
                _events.Add(1, new KeyValuePair<string, object?>("command", command.FullName), new("category", "measurement_unavailable"), new("model", model?.FullName), new("reason", failure.GetType().Name));
            }
            catch (Exception) { Interlocked.Increment(ref _instrumentationFailures); }
            try { logger?.Unavailable(command.FullName, failure.GetType().Name); }
            catch (Exception) { Interlocked.Increment(ref _instrumentationFailures); }
        }
    }
}

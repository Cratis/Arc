// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

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
    internal static void Record(Type command, string category, Type? model = null, string? reason = null)
    {
        try
        {
            _events.Add(1, new KeyValuePair<string, object?>("command", command.FullName), new("category", category), new("model", model?.FullName), new("reason", reason));
            Activity.Current?.AddEvent(new ActivityEvent("read-protection.plan", tags: new ActivityTagsCollection
            {
                { "command", command.FullName },
                { "category", category },
                { "model", model?.FullName },
                { "reason", reason }
            }));
        }
        catch (Exception)
        {
            // Keep failure accounting local: notifying another observer here could fail again.
            Interlocked.Increment(ref _instrumentationFailures);
        }
    }
}

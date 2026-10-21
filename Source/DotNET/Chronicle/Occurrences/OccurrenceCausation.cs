// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Describes the <see cref="Causation"/> entry recorded for an occurrence the executor runs a command for.
/// </summary>
public static class OccurrenceCausation
{
    /// <summary>
    /// The <see cref="CausationType"/> of an occurrence.
    /// </summary>
    public static readonly CausationType Type = "Occurrence";

    /// <summary>
    /// Builds the causation properties for an occurrence effect.
    /// </summary>
    /// <param name="occurrence">The <see cref="OccurrenceContext"/>.</param>
    /// <param name="key">The <see cref="EffectKey"/>.</param>
    /// <returns>The properties.</returns>
    public static IDictionary<string, string> PropertiesFor(OccurrenceContext occurrence, EffectKey key)
    {
        var properties = new Dictionary<string, string>
        {
            ["occurrenceId"] = occurrence.Id.Value,
            ["triggerId"] = occurrence.Trigger.Value,
            ["kind"] = occurrence.Kind.ToString(),
            ["recordedAt"] = occurrence.RecordedAt.ToString("O"),
            ["attempt"] = occurrence.Attempt.ToString(),
            ["epoch"] = occurrence.Lease.Epoch.ToString(),
            ["effectKey"] = key.Value
        };
        if (occurrence.DueAt is { } dueAt)
        {
            properties["dueAt"] = dueAt.ToString("O");
        }

        return properties;
    }
}

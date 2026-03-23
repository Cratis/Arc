// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace AspNetCore;

/// <summary>
/// Represents a live counter that ticks every second.
/// </summary>
/// <param name="Count">The current count value.</param>
/// <param name="LastUpdated">The timestamp of the last update.</param>
[ReadModel]
public record Ticker(int Count, DateTimeOffset LastUpdated)
{
    static readonly BehaviorSubject<Ticker> _subject = new(new Ticker(0, DateTimeOffset.UtcNow));

    static Ticker()
    {
        // Tick every second so the client sees updates without any user interaction.
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var current = _subject.Value;
                _subject.OnNext(new Ticker(current.Count + 1, DateTimeOffset.UtcNow));
            }
        });
    }

    /// <summary>
    /// Observes the live counter value.
    /// </summary>
    /// <returns>An observable that emits a new <see cref="Ticker"/> each second.</returns>
    public static ISubject<Ticker> Observe() => _subject;
}

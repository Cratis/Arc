// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace AspNetCore;

/// <summary>
/// Represents a live feed observable read model.
/// </summary>
/// <param name="Author">The author of the message.</param>
/// <param name="Text">The message text.</param>
/// <param name="PostedAt">The timestamp when the message was posted.</param>
[ReadModel]
public record LiveFeed(string Author, string Text, DateTimeOffset PostedAt)
{
    static readonly BehaviorSubject<IEnumerable<LiveFeed>> _all = new([
        new LiveFeed("system", "Feed started", DateTimeOffset.UtcNow)
    ]);

    /// <summary>
    /// Observes all messages in the live feed.
    /// </summary>
    /// <returns>An observable that emits the full list whenever a message is posted.</returns>
    public static ISubject<IEnumerable<LiveFeed>> All()
    {
        var relay = new BehaviorSubject<IEnumerable<LiveFeed>>(_all.Value);
        _all.Subscribe(relay);
        return relay;
    }

    /// <summary>
    /// Observes messages posted by a specific author.
    /// </summary>
    /// <param name="author">The author to filter by.</param>
    /// <returns>An observable that emits matching messages.</returns>
    public static ISubject<IEnumerable<LiveFeed>> ByAuthor(string author)
    {
        var relay = new BehaviorSubject<IEnumerable<LiveFeed>>(
            _all.Value.Where(m => m.Author == author));
        _all.Subscribe(items => relay.OnNext(items.Where(m => m.Author == author)));
        return relay;
    }

    /// <summary>
    /// Posts a new message to the feed, pushing it to all subscribers.
    /// </summary>
    /// <param name="author">Message author.</param>
    /// <param name="text">Message text.</param>
    internal static void Post(string author, string text) =>
        _all.OnNext(_all.Value.Append(new LiveFeed(author, text, DateTimeOffset.UtcNow)));
}

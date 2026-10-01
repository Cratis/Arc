// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.MongoDB;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace AspNetCore.Features.MongoWatcher;

/// <summary>
/// Represents an item stored in the watcher demo collection.
/// </summary>
/// <param name="Name">The display name of the item.</param>
/// <param name="CreatedAt">The UTC timestamp when the item was added.</param>
[ReadModel, AllowAnonymous]
public record WatcherItem(string Name, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Observes all items using the shared database-level change stream via <see cref="IMongoDBWatcher"/>.
    /// The subject also re-emits when <see cref="WatcherNote"/> documents change, because both
    /// collections share the same underlying change stream connection.
    /// </summary>
    /// <param name="watcher">The singleton watcher that holds one change stream per database.</param>
    /// <returns>
    /// An <see cref="ISubject{T}"/> that emits the full item list whenever <see cref="WatcherItem"/>
    /// or <see cref="WatcherNote"/> documents change.
    /// </returns>
    public static ISubject<IEnumerable<WatcherItem>> WatchAll(IMongoDBWatcher watcher) =>
        watcher
            .Observe<WatcherItem>()
            .Join<WatcherNote>()
            .Select((items, _) => items);

    /// <summary>
    /// Observes all items using a dedicated per-collection change stream opened directly on
    /// the <see cref="WatcherItem"/> MongoDB collection. This is the traditional single-collection
    /// approach — useful for comparison with the shared <see cref="WatchAll"/> stream.
    /// </summary>
    /// <param name="items">The MongoDB collection for watcher items.</param>
    /// <returns>
    /// An <see cref="ISubject{T}"/> that emits the full item list whenever a <see cref="WatcherItem"/>
    /// document changes. It does not react to changes in other collections.
    /// </returns>
    public static ISubject<IEnumerable<WatcherItem>> ObserveAll(IMongoCollection<WatcherItem> items) =>
        items.Observe();
}

/// <summary>
/// Represents a note stored in the secondary collection for the watcher demo.
/// Demonstrates that changes to this collection also trigger re-emission of <see cref="WatcherItem.WatchAll"/>,
/// because both collections are observed through the same shared database-level change stream.
/// </summary>
/// <param name="Text">The note text.</param>
/// <param name="CreatedAt">The UTC timestamp when the note was added.</param>
[ReadModel, AllowAnonymous]
public record WatcherNote(string Text, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Observes all notes using the shared database-level change stream.
    /// The subject also re-emits when <see cref="WatcherItem"/> documents change.
    /// </summary>
    /// <param name="watcher">The singleton watcher that holds one change stream per database.</param>
    /// <returns>
    /// An <see cref="ISubject{T}"/> that emits the full note list whenever <see cref="WatcherNote"/>
    /// or <see cref="WatcherItem"/> documents change.
    /// </returns>
    public static ISubject<IEnumerable<WatcherNote>> WatchAll(IMongoDBWatcher watcher) =>
        watcher
            .Observe<WatcherNote>()
            .Join<WatcherItem>()
            .Select((notes, _) => notes);

    /// <summary>
    /// Observes all notes using a dedicated per-collection change stream.
    /// </summary>
    /// <param name="notes">The MongoDB collection for watcher notes.</param>
    /// <returns>
    /// An <see cref="ISubject{T}"/> that emits the full note list whenever a <see cref="WatcherNote"/>
    /// document changes.
    /// </returns>
    public static ISubject<IEnumerable<WatcherNote>> ObserveAll(IMongoCollection<WatcherNote> notes) =>
        notes.Observe();
}

/// <summary>
/// Represents a command to add a new item to the watcher demo collection.
/// </summary>
/// <param name="Name">The display name of the item to add.</param>
[Command, AllowAnonymous]
public record AddWatcherItem(string Name)
{
    /// <summary>
    /// Handles the command by inserting a new item into the MongoDB collection.
    /// </summary>
    /// <param name="items">The MongoDB collection for watcher items.</param>
    public void Handle(IMongoCollection<WatcherItem> items) =>
        items.InsertOne(new WatcherItem(Name, DateTimeOffset.UtcNow));
}

/// <summary>
/// Represents a command to add a new note to the secondary watcher demo collection.
/// </summary>
/// <param name="Text">The text content of the note to add.</param>
[Command, AllowAnonymous]
public record AddWatcherNote(string Text)
{
    /// <summary>
    /// Handles the command by inserting a new note into the MongoDB collection.
    /// </summary>
    /// <param name="notes">The MongoDB collection for watcher notes.</param>
    public void Handle(IMongoCollection<WatcherNote> notes) =>
        notes.InsertOne(new WatcherNote(Text, DateTimeOffset.UtcNow));
}

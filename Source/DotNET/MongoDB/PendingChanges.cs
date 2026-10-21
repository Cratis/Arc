// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Holds the changes an observation has not applied yet, collapsed so that how much it holds does not grow with how
/// many changes arrive while it is busy.
/// </summary>
/// <param name="readsAgainOnEveryChange">Whether any change is answered by reading the observation again, as a paged observation is.</param>
/// <param name="maximumDocuments">How many changed documents are kept before a single read again replaces them.</param>
/// <remarks>
/// <para>
/// The shared change stream delivers every change in the database, and an observation applies them one at a time with a
/// query of its own. A change the observation has not reached yet only matters for what it leaves behind, so only the latest
/// change to each document is kept: the change stream carries the document as it is after the change, and every change
/// is applied against the collection as it is now.
/// </para>
/// <para>
/// When the observation is read again anyway - it is paged, the shared stream was reopened, or more documents changed than
/// are kept - the changes are dropped and one read again stands for all of them. The read happens after every change it
/// replaces, so it sees them.
/// </para>
/// </remarks>
internal sealed class PendingChanges(bool readsAgainOnEveryChange, int maximumDocuments = PendingChanges.MaximumDocuments)
{
    /// <summary>
    /// The number of changed documents kept before one read again replaces them.
    /// </summary>
    internal const int MaximumDocuments = 512;

    readonly object _gate = new();
    readonly Dictionary<BsonValue, LinkedListNode<ChangeStreamDocument<BsonDocument>>> _byDocument = [];
    readonly LinkedList<ChangeStreamDocument<BsonDocument>> _inOrder = [];
    readonly Channel<bool> _ready = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    bool _readAgain;

    /// <summary>
    /// Gets the number of changes held.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (_gate)
            {
                return _inOrder.Count;
            }
        }
    }

    /// <summary>
    /// Adds a change.
    /// </summary>
    /// <param name="change">The change.</param>
    /// <param name="requiresReadingAgain">Whether the change can only be answered by reading the observation again.</param>
    internal void Add(ChangeStreamDocument<BsonDocument> change, bool requiresReadingAgain)
    {
        lock (_gate)
        {
            if (requiresReadingAgain || readsAgainOnEveryChange)
            {
                ReadAgain();
            }
            else if (!_readAgain)
            {
                // A change naming no document has nothing to apply.
                if (change.DocumentKey is null || !change.DocumentKey.TryGetValue("_id", out var id))
                {
                    return;
                }

                if (_byDocument.Remove(id, out var earlier))
                {
                    _inOrder.Remove(earlier);
                }

                if (_byDocument.Count >= maximumDocuments)
                {
                    ReadAgain();
                }
                else
                {
                    _byDocument[id] = _inOrder.AddLast(change);
                }
            }
        }

        _ready.Writer.TryWrite(true);
    }

    /// <summary>
    /// Waits until there are changes to take, or the changes have ended.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> ending the wait.</param>
    /// <returns>False when the changes have ended; throws the error they ended with, if any.</returns>
    internal ValueTask<bool> WaitForChanges(CancellationToken cancellationToken) => _ready.Reader.WaitToReadAsync(cancellationToken);

    /// <summary>
    /// Takes every change held.
    /// </summary>
    /// <returns>Whether to read the observation again, and otherwise the changes to apply, in the order they last happened.</returns>
    internal (bool ReadAgain, IReadOnlyList<ChangeStreamDocument<BsonDocument>> Changes) Take()
    {
        _ready.Reader.TryRead(out _);
        lock (_gate)
        {
            var taken = (_readAgain, (IReadOnlyList<ChangeStreamDocument<BsonDocument>>)[.. _inOrder]);
            _readAgain = false;
            _byDocument.Clear();
            _inOrder.Clear();
            return taken;
        }
    }

    /// <summary>
    /// Ends the changes once the held ones are taken.
    /// </summary>
    /// <param name="error">The error they ended with, if any.</param>
    internal void Complete(Exception? error = null) => _ready.Writer.TryComplete(error);

    void ReadAgain()
    {
        _readAgain = true;
        _byDocument.Clear();
        _inOrder.Clear();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents a <see cref="IChangeStreamCursor{TDocument}"/> that retries creating the actual cursor until the collection exists.
/// </summary>
/// <typeparam name="TDocument">Type of document.</typeparam>
/// <param name="createCursor">Opens the change stream, which is retried until the collection exists.</param>
/// <param name="retryInterval">The interval between retry attempts.</param>
/// <remarks>
/// Used to handle Azure Cosmos DB's "Collection not found" errors by periodically retrying the watch operation
/// until the collection is created, maintaining the observation alive.
/// </remarks>
public class RetryingChangeStreamCursor<TDocument>(Func<Task<IChangeStreamCursor<TDocument>>> createCursor, TimeSpan retryInterval) : IChangeStreamCursor<TDocument>
{
    IChangeStreamCursor<TDocument>? _actualCursor;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryingChangeStreamCursor{TDocument}"/> class.
    /// </summary>
    /// <param name="invocation">The original invocation to retry.</param>
    /// <param name="retryInterval">The interval between retry attempts.</param>
    public RetryingChangeStreamCursor(IInvocation invocation, TimeSpan retryInterval)
        : this(() => (Task<IChangeStreamCursor<TDocument>>)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments)!, retryInterval)
    {
    }

    /// <inheritdoc/>
    public IEnumerable<TDocument> Current => _actualCursor?.Current ?? [];

    /// <inheritdoc/>
    public BsonDocument? GetResumeToken() => _actualCursor?.GetResumeToken();

    /// <inheritdoc/>
    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        try
        {
            return MoveNextAsync(cancellationToken).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        while (!_disposed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_actualCursor is null)
            {
                if (await TryCreateActualCursor())
                {
                    return await _actualCursor!.MoveNextAsync(cancellationToken);
                }

                await Task.Delay(retryInterval, cancellationToken);
            }
            else
            {
                return await _actualCursor.MoveNextAsync(cancellationToken);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _actualCursor?.Dispose();
    }

    async Task<bool> TryCreateActualCursor()
    {
        try
        {
            _actualCursor = await createCursor().ConfigureAwait(false);
            return true;
        }
        catch (MongoCommandException ex) when (ex.Message.Contains(WellKnownErrorMessages.CollectionNotFound, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}

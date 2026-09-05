// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Atomically tracks the latest revision and operation for one query on one multiplexed connection.
/// </summary>
internal sealed class ObservableQuerySubscriptionState : IDisposable
{
    readonly object _sync = new();
    ObservableQuerySubscriptionOperation? _operation;
    long? _revision;
    bool _isRevisionAware;
    bool _isTombstone;
    DateTimeOffset? _tombstonedAt;

    /// <summary>
    /// Gets whether the state owns an active operation.
    /// </summary>
    public bool IsActive
    {
        get
        {
            lock (_sync)
            {
                return _operation is not null && !_isTombstone;
            }
        }
    }

    /// <summary>
    /// Gets whether this is an inactive legacy state that carries no revision ordering information.
    /// </summary>
    public bool IsInactiveLegacy
    {
        get
        {
            lock (_sync)
            {
                return _operation is null && !_isRevisionAware;
            }
        }
    }

    /// <summary>
    /// Attempts to reserve a subscribe operation.
    /// </summary>
    /// <param name="revision">The optional protocol revision.</param>
    /// <param name="connectionToken">The connection cancellation token.</param>
    /// <returns>The newly-owned operation, or <see langword="null"/> for a duplicate, stale, or incompatible legacy subscribe.</returns>
    public ObservableQuerySubscriptionOperation? TrySubscribe(long? revision, CancellationToken connectionToken)
    {
        ObservableQuerySubscriptionOperation? replaced;
        ObservableQuerySubscriptionOperation operation;

        lock (_sync)
        {
            if (revision is not null)
            {
                if (_isRevisionAware && revision <= _revision)
                {
                    return null;
                }

                _isRevisionAware = true;
                _revision = revision;
            }
            else if (_isRevisionAware)
            {
                return null;
            }

            operation = new ObservableQuerySubscriptionOperation(revision, connectionToken);
            replaced = _operation;
            _operation = operation;
            _isTombstone = false;
            _tombstonedAt = null;
        }

        replaced?.Dispose();
        return operation;
    }

    /// <summary>
    /// Determines whether an operation still owns this query.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns><see langword="true"/> when it is current.</returns>
    public bool IsCurrent(ObservableQuerySubscriptionOperation operation)
    {
        lock (_sync)
        {
            return ReferenceEquals(_operation, operation) && !_isTombstone;
        }
    }

    /// <summary>
    /// Terminates an operation only when it still owns this query.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="now">The time at which a revision-aware operation becomes a tombstone.</param>
    /// <returns><see langword="true"/> when the operation was terminated.</returns>
    public bool TryTerminate(ObservableQuerySubscriptionOperation operation, DateTimeOffset now)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_operation, operation))
            {
                return false;
            }

            _operation = null;
            _isTombstone = _isRevisionAware;
            _tombstonedAt = _isTombstone ? now : null;
        }

        operation.Dispose();
        return true;
    }

    /// <summary>
    /// Applies an unsubscribe using exact-revision ordering semantics.
    /// </summary>
    /// <param name="revision">The optional protocol revision.</param>
    /// <param name="now">The time at which the state becomes a tombstone.</param>
    /// <returns><see langword="true"/> when the state accepted the unsubscribe.</returns>
    public bool TryUnsubscribe(long? revision, DateTimeOffset now)
    {
        ObservableQuerySubscriptionOperation? operation;

        lock (_sync)
        {
            if (revision is not null)
            {
                if (_isRevisionAware && revision < _revision)
                {
                    return false;
                }

                if (_isRevisionAware && revision == _revision && _isTombstone)
                {
                    return true;
                }

                _isRevisionAware = true;
                _revision = revision;
                _isTombstone = true;
                _tombstonedAt = now;
                operation = _operation;
                _operation = null;
            }
            else
            {
                if (_isRevisionAware)
                {
                    return false;
                }

                operation = _operation;
                _operation = null;
                _tombstonedAt = null;
            }
        }

        operation?.Dispose();
        return true;
    }

    /// <summary>
    /// Gets the time at which this state became a revision tombstone.
    /// </summary>
    /// <param name="tombstonedAt">The tombstone timestamp when present.</param>
    /// <returns><see langword="true"/> when this state is a revision tombstone.</returns>
    public bool TryGetTombstonedAt(out DateTimeOffset tombstonedAt)
    {
        lock (_sync)
        {
            if (_isTombstone && _tombstonedAt is not null)
            {
                tombstonedAt = _tombstonedAt.Value;
                return true;
            }

            tombstonedAt = default;
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ObservableQuerySubscriptionOperation? operation;
        lock (_sync)
        {
            operation = _operation;
            _operation = null;
            _isTombstone = true;
            _tombstonedAt = null;
        }

        operation?.Dispose();
    }
}

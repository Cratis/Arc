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
    /// <returns><see langword="true"/> when the operation was terminated.</returns>
    public bool TryTerminate(ObservableQuerySubscriptionOperation operation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_operation, operation))
            {
                return false;
            }

            _operation = null;
            _isTombstone = _isRevisionAware;
        }

        operation.Dispose();
        return true;
    }

    /// <summary>
    /// Applies an unsubscribe using exact-revision ordering semantics.
    /// </summary>
    /// <param name="revision">The optional protocol revision.</param>
    /// <returns><see langword="true"/> when the state accepted the unsubscribe.</returns>
    public bool TryUnsubscribe(long? revision)
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
            }
        }

        operation?.Dispose();
        return true;
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
        }

        operation?.Dispose();
    }
}

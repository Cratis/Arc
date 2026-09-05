// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents one reserved server-side subscription operation for a query id.
/// </summary>
/// <remarks>
/// The reservation exists before asynchronous subscription creation starts. Replacing it cancels stale creation and
/// makes all stale callbacks fail their identity check. A subscription created after replacement is disposed immediately.
/// </remarks>
internal sealed class ObservableQuerySubscriptionOperation : IDisposable
{
    readonly object _sync = new();
    readonly CancellationTokenSource _cancellationTokenSource;
    IDisposable? _subscription;
    Action? _unregister;
    bool _creationCompleted;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQuerySubscriptionOperation"/> class.
    /// </summary>
    /// <param name="revision">The optional client revision.</param>
    /// <param name="connectionToken">The connection cancellation token.</param>
    public ObservableQuerySubscriptionOperation(long? revision, CancellationToken connectionToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectionToken);
        Token = _cancellationTokenSource.Token;
        Revision = revision;
    }

    /// <summary>
    /// Gets the optional client revision carried on the wire.
    /// </summary>
    public long? Revision { get; }

    /// <summary>
    /// Gets the operation cancellation token.
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Attempts to attach the created stream subscription to this reservation.
    /// </summary>
    /// <param name="subscription">The created subscription.</param>
    /// <returns><see langword="true"/> if the reservation still owns the subscription; otherwise <see langword="false"/>.</returns>
    public bool TryAttach(IDisposable subscription)
    {
        lock (_sync)
        {
            if (!_disposed && !_cancellationTokenSource.IsCancellationRequested)
            {
                _subscription = subscription;
                return true;
            }
        }

        subscription.Dispose();
        return false;
    }

    /// <summary>
    /// Registers subscription health while serializing registration against replacement disposal.
    /// </summary>
    /// <param name="register">Registers the subscription.</param>
    /// <param name="unregister">Unregisters the subscription during disposal.</param>
    /// <returns><see langword="true"/> if registration occurred; otherwise <see langword="false"/>.</returns>
    public bool TryRegister(Action register, Action unregister)
    {
        lock (_sync)
        {
            if (_disposed || _cancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            register();
            _unregister = unregister;
            return true;
        }
    }

    /// <summary>
    /// Marks asynchronous creation as completed, allowing the operation token source to be released after disposal.
    /// </summary>
    public void CompleteCreation()
    {
        var disposeCancellation = false;
        lock (_sync)
        {
            _creationCompleted = true;
            disposeCancellation = _disposed;
        }

        if (disposeCancellation)
        {
            _cancellationTokenSource.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IDisposable? subscription;
        Action? unregister;
        var disposeCancellation = false;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            subscription = _subscription;
            _subscription = null;
            unregister = _unregister;
            _unregister = null;
            disposeCancellation = _creationCompleted;
        }

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already completed and disposed.
        }

        subscription?.Dispose();
        unregister?.Invoke();

        if (disposeCancellation)
        {
            _cancellationTokenSource.Dispose();
        }
    }
}

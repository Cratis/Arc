// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Owns the cancellation and resources of one streaming observable query subscription.
/// </summary>
/// <remarks>
/// Disposal first cancels the subscription and detaches its producer. Gates, service scopes and the cancellation
/// source are disposed only after every callback or background stream that entered the lifetime has exited.
/// </remarks>
internal sealed class StreamingQuerySubscription : IDisposable
{
    readonly object _sync = new();
    readonly List<IDisposable> _resources = [];
    readonly Action _cancel;
    readonly Action<Exception>? _onCleanupFailure;
    readonly TaskCompletionSource _resourcesDisposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IDisposable? _producer;
    int _activeOperations;
    bool _disposeRequested;
    bool _resourcesTaken;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingQuerySubscription"/> class.
    /// </summary>
    /// <param name="connectionToken">The connection or operation token this subscription is linked to.</param>
    /// <param name="onCleanupFailure">Reports a failure while disposing an owned resource.</param>
    public StreamingQuerySubscription(CancellationToken connectionToken, Action<Exception>? onCleanupFailure = null)
    {
        _onCleanupFailure = onCleanupFailure;
        var ownedCancellation = (CancellationTokenSource?)CancellationTokenSource.CreateLinkedTokenSource(connectionToken);
        try
        {
            Token = ownedCancellation.Token;
            _cancel = ownedCancellation.Cancel;
            _resources.Add(ownedCancellation);
            ownedCancellation = null;
        }
        finally
        {
            ownedCancellation?.Dispose();
        }
    }

    /// <summary>
    /// Gets the token cancelled when this subscription stops.
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Gets a task that completes once the owned resources have been disposed. It never faults.
    /// </summary>
    internal Task ResourcesDisposed => _resourcesDisposed.Task;

    /// <summary>
    /// Adds a resource whose lifetime extends until all active work has exited.
    /// </summary>
    /// <param name="resource">The resource to own.</param>
    public void AddResource(IDisposable resource)
    {
        var disposeImmediately = false;
        lock (_sync)
        {
            if (_disposeRequested)
            {
                disposeImmediately = true;
            }
            else
            {
                _resources.Add(resource);
            }
        }

        if (disposeImmediately)
        {
            _ = DisposeLateResourceAsync(resource);
        }
    }

    /// <summary>
    /// Sets the subject observer that must be detached as soon as disposal starts.
    /// </summary>
    /// <param name="producer">The producer subscription.</param>
    public void SetProducer(IDisposable producer)
    {
        var disposeImmediately = false;
        lock (_sync)
        {
            if (_disposeRequested)
            {
                disposeImmediately = true;
            }
            else
            {
                _producer = producer;
            }
        }

        if (disposeImmediately)
        {
            producer.Dispose();
        }
    }

    /// <summary>
    /// Attempts to enter an operation that uses subscription-owned resources.
    /// </summary>
    /// <returns><see langword="true"/> when the operation may run; otherwise <see langword="false"/>.</returns>
    public bool TryEnter()
    {
        lock (_sync)
        {
            if (_disposeRequested)
            {
                return false;
            }

            _activeOperations++;
            return true;
        }
    }

    /// <summary>
    /// Leaves an active operation and completes deferred resource disposal when it was the last one.
    /// </summary>
    public void Exit()
    {
        List<IDisposable>? resources = null;
        lock (_sync)
        {
            _activeOperations--;
            if (_disposeRequested && _activeOperations == 0)
            {
                resources = TakeResourcesForDisposal();
            }
        }

        StartDisposingResources(resources);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IDisposable? producer;
        List<IDisposable>? resources = null;

        lock (_sync)
        {
            if (_disposeRequested)
            {
                return;
            }

            _disposeRequested = true;
            producer = _producer;
            _producer = null;
        }

        try
        {
            _cancel();
        }
        catch (ObjectDisposedException)
        {
            // The deferred cleanup already completed.
        }

        // Stop new observer callbacks before considering gates and scopes for disposal.
        producer?.Dispose();

        lock (_sync)
        {
            if (_activeOperations == 0)
            {
                resources = TakeResourcesForDisposal();
            }
        }

        StartDisposingResources(resources);
    }

    List<IDisposable>? TakeResourcesForDisposal()
    {
        if (_resourcesTaken)
        {
            return null;
        }

        _resourcesTaken = true;
        var resources = new List<IDisposable>(_resources);
        _resources.Clear();
        return resources;
    }

    void StartDisposingResources(List<IDisposable>? resources)
    {
        if (resources is null)
        {
            return;
        }

        _ = DisposeResourcesAsync(resources);
    }

    async Task DisposeResourcesAsync(List<IDisposable> resources)
    {
        foreach (var resource in resources)
        {
            await DisposeLateResourceAsync(resource);
        }

        _resourcesDisposed.TrySetResult();
    }

    async Task DisposeLateResourceAsync(IDisposable resource)
    {
        try
        {
            await SubscriptionResourceDisposal.DisposeAsync(resource);
        }
        catch (Exception error) when (!ObservableQueryDemultiplexer.IsFatal(error))
        {
            _onCleanupFailure?.Invoke(error);
        }
    }
}

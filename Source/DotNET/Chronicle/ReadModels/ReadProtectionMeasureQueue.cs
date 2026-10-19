// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Runs sampled scans outside invocation flows, with bounded retention and a five-second shutdown drain.
/// </summary>
internal sealed class ReadProtectionMeasureQueue : IHostedService, IDisposable
{
    readonly Channel<ReadProtectionMeasureScan> _queue = Channel.CreateBounded<ReadProtectionMeasureScan>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            AllowSynchronousContinuations = false
        },
        dropped => ReadProtectionTelemetry.Record(dropped.Command, "measurement_unavailable", reason: "Scan queue dropped the oldest observation", logger: dropped.Logger));
    readonly CancellationTokenSource _stop = new();
    readonly object _lifetime = new();
    Task? _worker;
    bool _disposed;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Never inherit a command's transaction, tenant scope, Activity, or protection frame into the worker.
        // The token is taken here, once: reading it from a disposed source would throw inside the worker.
        var stopping = _stop.Token;
        using (ExecutionContext.SuppressFlow()) _worker = Task.Run(() => Drain(stopping), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // A host may stop more than once, or after the container has disposed the queue; neither may fail shutdown.
        _queue.Writer.TryComplete();
        if (!TryCancel(_ => _.CancelAfter(TimeSpan.FromSeconds(5))) || _worker is null) return;
        try { await _worker.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
        catch (Exception failure) when (failure is OperationCanceledException or TimeoutException) { TryCancel(_ => _.Cancel()); }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _queue.Writer.TryComplete();
        lock (_lifetime)
        {
            if (_disposed) return;
            _disposed = true;

            // A worker still draining when the queue is disposed without being stopped ends now instead of running on.
            _stop.Cancel();
            _stop.Dispose();
        }
    }

    /// <summary>
    /// Adds one immutable observation without waiting for the worker.
    /// </summary>
    /// <param name="scan">The bounded scan.</param>
    /// <returns>Whether it was accepted.</returns>
    internal bool Enqueue(ReadProtectionMeasureScan scan) => _worker?.IsCompleted == false && _queue.Writer.TryWrite(scan);

    bool TryCancel(Action<CancellationTokenSource> cancel)
    {
        lock (_lifetime)
        {
            if (_disposed) return false;
            cancel(_stop);
            return true;
        }
    }

    async Task Drain(CancellationToken stopping)
    {
        try
        {
            await foreach (var scan in _queue.Reader.ReadAllAsync(stopping)) await scan.Run(stopping);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // Shutdown has reached its bounded drain deadline; remaining reports are unavailable.
            while (_queue.Reader.TryRead(out var scan)) ReadProtectionTelemetry.Record(scan.Command, "measurement_unavailable", reason: "Shutdown drain deadline", logger: scan.Logger);
        }
    }
}

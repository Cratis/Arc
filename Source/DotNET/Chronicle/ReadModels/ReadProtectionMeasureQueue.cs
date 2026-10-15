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
    Task? _worker;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Never inherit a command's transaction, tenant scope, Activity, or protection frame into the worker.
        using (ExecutionContext.SuppressFlow()) _worker = Task.Run(Drain, CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();
        _stop.CancelAfter(TimeSpan.FromSeconds(5));
        if (_worker is not null)
        {
            try { await _worker.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
            catch (Exception failure) when (failure is OperationCanceledException or TimeoutException) { await _stop.CancelAsync(); }
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _stop.Dispose();

    /// <summary>
    /// Adds one immutable observation without waiting for the worker.
    /// </summary>
    /// <param name="scan">The bounded scan.</param>
    /// <returns>Whether it was accepted.</returns>
    internal bool Enqueue(ReadProtectionMeasureScan scan) => _worker?.IsCompleted == false && _queue.Writer.TryWrite(scan);

    async Task Drain()
    {
        try
        {
            await foreach (var scan in _queue.Reader.ReadAllAsync(_stop.Token)) await scan.Run(_stop.Token);
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            // Shutdown has reached its bounded drain deadline; remaining reports are unavailable.
            while (_queue.Reader.TryRead(out var scan)) ReadProtectionTelemetry.Record(scan.Command, "measurement_unavailable", reason: "Shutdown drain deadline", logger: scan.Logger);
        }
    }
}

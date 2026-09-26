// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Captures receipt at an Arc dispatch boundary, independently of the service scope used later for execution.
/// </summary>
internal static class OperationContextScope
{
    static readonly AsyncLocal<DateTimeOffset?> _receivedAt = new();
    static readonly AsyncLocal<ForwardingState?> _forwardTransportReceipt = new();
    static readonly AsyncLocal<bool> _insidePipelineEntry = new();

    /// <summary>Gets the receipt time for the active operation.</summary>
    internal static DateTimeOffset? Current => _receivedAt.Value;

    /// <summary>Starts a new operation with the configured clock and restores the previous one on disposal.</summary>
    /// <param name="services">The provider containing the operation's clock.</param>
    /// <returns>The operation lease.</returns>
    internal static IDisposable Begin(IServiceProvider? services)
    {
        var previous = _receivedAt.Value;
        _receivedAt.Value = (services?.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System).GetUtcNow();
        return new Restore(previous);
    }

    /// <summary>Preserves a receipt already captured by the transport, or starts a direct operation.</summary>
    /// <param name="services">The provider containing the operation's clock.</param>
    /// <returns>A lease if this entry captured a receipt, otherwise null.</returns>
    internal static IDisposable? BeginIfNotSet(IServiceProvider? services) => Current is null ? Begin(services) : null;

    /// <summary>Offers the transport receipt to public pipeline entries in a decorated dispatch.</summary>
    /// <returns>A lease that stops forwarding after the dispatch.</returns>
    internal static IDisposable ForwardTransportReceipt()
    {
        var previous = _forwardTransportReceipt.Value;
        var offer = new ForwardingState();
        _forwardTransportReceipt.Value = offer;
        return new RestoreForwarding(previous, offer);
    }

    /// <summary>Reuses an active transport receipt at sibling pipeline entries, or starts a direct or nested operation.</summary>
    /// <param name="services">The provider containing the operation's clock.</param>
    /// <returns>A lease restoring the pipeline entry and any receipt it captured.</returns>
    internal static IDisposable BeginPipeline(IServiceProvider? services)
    {
        var insidePipelineEntry = _insidePipelineEntry.Value;
        IDisposable? receipt = null;
        if (_forwardTransportReceipt.Value is not { IsEnded: false } || insidePipelineEntry || Current is null)
        {
            receipt = Begin(services);
        }

        _insidePipelineEntry.Value = true;
        return new RestorePipeline(insidePipelineEntry, receipt);
    }

    sealed class ForwardingState
    {
        int _ended;

        public bool IsEnded => Volatile.Read(ref _ended) != 0;

        public void End() => Interlocked.Exchange(ref _ended, 1);
    }

    sealed class RestoreForwarding(ForwardingState? previous, ForwardingState offer) : IDisposable
    {
        public void Dispose()
        {
            offer.End();
            _forwardTransportReceipt.Value = previous;
        }
    }

    sealed class RestorePipeline(bool previous, IDisposable? receipt) : IDisposable
    {
        public void Dispose()
        {
            _insidePipelineEntry.Value = previous;
            receipt?.Dispose();
        }
    }

    sealed class Restore(DateTimeOffset? previous) : IDisposable
    {
        public void Dispose() => _receivedAt.Value = previous;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Captures receipt at an Arc dispatch boundary, independently of the service scope used later for execution.
/// </summary>
public static class OperationContextScope
{
    static readonly AsyncLocal<Operation?> _operation = new();
    static readonly AsyncLocal<ForwardingState?> _forwardTransportReceipt = new();
    static readonly AsyncLocal<bool> _insidePipelineEntry = new();

    /// <summary>Gets an opaque lifetime key for the active dispatch, or null outside a dispatch.</summary>
    /// <remarks>Equal receipt times do not imply equal operations. This key is never a caller identity or a grant.</remarks>
    public static object? Identity => _operation.Value;

    /// <summary>Gets the receipt time for the active operation.</summary>
    internal static DateTimeOffset? Current => _operation.Value?.ReceivedAt;

    /// <summary>Starts a new operation with the configured clock and restores the previous one on disposal.</summary>
    /// <param name="services">The provider containing the operation's clock.</param>
    /// <returns>The operation lease.</returns>
    public static IDisposable Begin(IServiceProvider? services)
    {
        return BeginAt((services?.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System).GetUtcNow());
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
        var receipt = _forwardTransportReceipt.Value is { IsEnded: false } && !insidePipelineEntry && Current is { } forwarded
            ? BeginAt(forwarded)
            : Begin(services);

        _insidePipelineEntry.Value = true;
        return new RestorePipeline(insidePipelineEntry, receipt);
    }

    static Restore BeginAt(DateTimeOffset receivedAt)
    {
        var previous = _operation.Value;
        _operation.Value = new(receivedAt);
        return new Restore(previous);
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

    sealed class Operation(DateTimeOffset receivedAt)
    {
        internal DateTimeOffset ReceivedAt => receivedAt;
    }

    sealed class Restore(Operation? previous) : IDisposable
    {
        public void Dispose() => _operation.Value = previous;
    }
}

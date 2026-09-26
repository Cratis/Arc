// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Captures receipt at an Arc dispatch boundary, independently of the service scope used later for execution.
/// </summary>
internal static class OperationContextScope
{
    static readonly AsyncLocal<DateTimeOffset?> _receivedAt = new();
    static readonly AsyncLocal<bool> _forwardTransportReceipt = new();

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

    /// <summary>Offers the transport receipt to the first public pipeline entry in a decorated dispatch.</summary>
    /// <returns>A lease that stops forwarding after the dispatch.</returns>
    internal static IDisposable ForwardTransportReceipt()
    {
        var previous = _forwardTransportReceipt.Value;
        _forwardTransportReceipt.Value = true;
        return new RestoreForwarding(previous);
    }

    /// <summary>Consumes a forwarded transport receipt once, or starts a new direct or nested operation.</summary>
    /// <param name="services">The provider containing the operation's clock.</param>
    /// <returns>A lease for a new receipt, or null when the transport owns it.</returns>
    internal static IDisposable? BeginPipeline(IServiceProvider? services)
    {
        if (_forwardTransportReceipt.Value && Current is not null)
        {
            _forwardTransportReceipt.Value = false;
            return null;
        }

        return Begin(services);
    }

    sealed class RestoreForwarding(bool previous) : IDisposable
    {
        public void Dispose() => _forwardTransportReceipt.Value = previous;
    }

    sealed class Restore(DateTimeOffset? previous) : IDisposable
    {
        public void Dispose() => _receivedAt.Value = previous;
    }
}

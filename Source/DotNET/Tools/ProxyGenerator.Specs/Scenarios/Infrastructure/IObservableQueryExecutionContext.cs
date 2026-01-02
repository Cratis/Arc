// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Defines the interface for observable query execution contexts.
/// Used for polymorphic access to SignalUpdateReceived without knowing the TResult type.
/// </summary>
public interface IObservableQueryExecutionContext
{
    /// <summary>
    /// Signals that an update has been received from the WebSocket.
    /// </summary>
    void SignalUpdateReceived();
}

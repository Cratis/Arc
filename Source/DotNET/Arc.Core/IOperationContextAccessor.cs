// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Provides the Arc receipt time of the operation currently executing in this asynchronous flow.
/// </summary>
public interface IOperationContextAccessor
{
    /// <summary>
    /// Gets the receipt time, or null when no Arc operation is active.
    /// </summary>
    DateTimeOffset? ReceivedAt { get; }
}

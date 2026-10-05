// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Optional capability of an <see cref="IAggregateRootContext"/> that carries the event source declared by the aggregate root.
/// </summary>
/// <remarks>
/// This is a separate interface so that existing <see cref="IAggregateRootContext"/> implementations keep compiling
/// unchanged. A context that does not implement it is treated as one without a declared event source.
/// </remarks>
public interface IAggregateRootEventSourceContext
{
    /// <summary>
    /// Gets the type of the event source definition the aggregate root declares, if any.
    /// </summary>
    Type? EventSource { get; }

    /// <summary>
    /// Gets the name of the stream the aggregate root declares on its event source, if any.
    /// </summary>
    string? EventStream { get; }
}

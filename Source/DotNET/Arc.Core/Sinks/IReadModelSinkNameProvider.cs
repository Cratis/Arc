// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks;

/// <summary>
/// Defines a source of the named sink a read model type is stored in, such as an event store client's own sink declarations.
/// </summary>
/// <remarks>
/// Register implementations in the service collection as <see cref="IReadModelSinkNameProvider"/>. They are asked in
/// registration order after <see cref="ReadModelSinkAttribute"/>, and the first name given wins.
/// </remarks>
public interface IReadModelSinkNameProvider
{
    /// <summary>
    /// Get the name of the sink a read model type is stored in.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The name of the sink, or null when this provider has no sink for the type.</returns>
    string? GetSinkNameFor(Type readModelType);
}

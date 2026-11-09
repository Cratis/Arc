// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks;

/// <summary>
/// Defines a system that knows which named sink each read model type is stored in.
/// </summary>
public interface IReadModelSinks
{
    /// <summary>
    /// Get the name of the sink a read model type is stored in.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The name of the sink, or null when the type is stored in the default sink.</returns>
    string? GetSinkNameFor(Type readModelType);
}

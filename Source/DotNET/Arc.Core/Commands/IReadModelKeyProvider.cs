// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Resolves a read-model key from the selected command caller.
/// Implementations must be stateless, thread-safe, synchronous, I/O-free and service-free.
/// </summary>
public interface IReadModelKeyProvider
{
    /// <summary>Resolves the key without consulting ambient identity or services.</summary>
    /// <param name="context">The immutable caller and target read-model type.</param>
    /// <returns>The lookup outcome.</returns>
    ReadModelKeyLookup Resolve(ReadModelKeyContext context);
}

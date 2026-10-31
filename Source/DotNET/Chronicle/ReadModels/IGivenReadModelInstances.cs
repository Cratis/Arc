// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Defines an internal testing hook for given state after a plain model's protected read has completed.
/// </summary>
internal interface IGivenReadModelInstances
{
    /// <summary>
    /// Tries to find an explicitly pinned instance without materializing a read model.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="key">The resolved read model key.</param>
    /// <param name="instance">The pinned instance, if present.</param>
    /// <returns>Whether an instance was pinned for this model and key.</returns>
    bool TryGet(Type readModelType, ReadModelKey key, [NotNullWhen(true)] out object? instance);
}

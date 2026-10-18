// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Retains the raw selected key alongside the exact target it originally resolved to.
/// </summary>
/// <param name="Value">The original key, including null.</param>
/// <param name="Target">The resolved target before any provider or response override.</param>
internal sealed record RawCommandSourceKey(object? Value, EventSourceId Target)
{
    /// <summary>
    /// The provider context slot for the retained key and its resolved target.
    /// </summary>
    internal const string ContextKey = "Cratis.Arc.Chronicle.RawCommandSourceKey";
}

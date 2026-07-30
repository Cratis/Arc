// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a command that composes the key identifying what it acts on.
/// </summary>
/// <remarks>
/// A command that is keyed by something other than one of its own properties — a composite of two of them, a value
/// derived from them — declares the key itself rather than leaving it to be inferred. This is what a read model backing
/// provider loads the read model by, so it is the same key in every store.
/// </remarks>
public interface ICanProvideKeyForCommand
{
    /// <summary>
    /// Gets the key identifying what the command acts on.
    /// </summary>
    /// <returns>The key, or null when the command carries nothing to compose one from.</returns>
    string? GetKey();
}

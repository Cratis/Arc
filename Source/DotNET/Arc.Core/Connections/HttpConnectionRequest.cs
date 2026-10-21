// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the body <see cref="HttpConnectionResolver"/> posts to its endpoint.
/// </summary>
/// <param name="Storage">The kind of storage to connect to.</param>
/// <param name="Purpose">The <see cref="ConnectionPurpose"/> of the connection.</param>
/// <param name="Scope">The values identifying what is being connected.</param>
public record HttpConnectionRequest(string Storage, ConnectionPurpose Purpose, IReadOnlyDictionary<string, string> Scope);

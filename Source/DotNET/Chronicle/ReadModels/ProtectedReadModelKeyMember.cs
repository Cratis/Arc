// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Describes a public readable instance property of an exact composite key type.
/// </summary>
/// <typeparam name="T">The exact key type.</typeparam>
/// <param name="Name">The property name.</param>
/// <param name="Read">The statically typed property getter.</param>
public sealed record ProtectedReadModelKeyMember<T>(string Name, Func<T, object?> Read);

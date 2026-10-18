// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when strict protected-key traversal has no accessors for an exact composite type.
/// </summary>
/// <param name="keyType">The uncovered CLR type, never a key value.</param>
public sealed class ProtectedReadModelKeyAccessorMissing(Type keyType)
    : Exception($"Protected read-model key type '{keyType}' has no composite accessors. Call ProtectedReadModelKeyAccessors.Register<T>() with every public readable non-indexed instance property before commands run.");

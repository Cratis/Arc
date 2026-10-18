// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a generated or manual composite descriptor conflicts with an existing registration.
/// </summary>
/// <param name="keyType">The exact key type already registered.</param>
public sealed class ProtectedReadModelKeyAccessorsAlreadyRegistered(Type keyType)
    : Exception($"Protected read-model key accessors for '{keyType}' are already registered. Register each exact type only once, including generated registrations.");

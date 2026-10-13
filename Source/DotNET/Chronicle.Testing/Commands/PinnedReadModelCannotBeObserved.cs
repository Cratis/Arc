// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when observation cannot read the key of a pinned read model.
/// </summary>
/// <param name="readModelType">The pinned read model type.</param>
public sealed class PinnedReadModelCannotBeObserved(Type readModelType)
    : Exception($"Pinned read model '{readModelType.FullName}' cannot be observed because its key is not readable. Seed events instead of pinning, or give the read model a key property.");

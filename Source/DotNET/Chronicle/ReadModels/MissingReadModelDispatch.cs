// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when read protection needs typed dispatch for a read model that was never registered.
/// </summary>
/// <param name="model">The read model type without dispatch.</param>
public sealed class MissingReadModelDispatch(Type model)
    : Exception($"Read model '{model}' has no typed read protection dispatch. Register it through AddReadModels, or call ReadModelDispatch.Register<T>() before commands run.");

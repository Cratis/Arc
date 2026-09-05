// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

/// <summary>
/// A read model materialized by a reducer.
/// </summary>
/// <param name="Value">The value of the read model.</param>
public record ReducerReadModel(string Value);

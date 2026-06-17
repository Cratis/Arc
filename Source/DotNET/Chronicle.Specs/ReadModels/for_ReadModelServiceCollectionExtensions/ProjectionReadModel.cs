// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

/// <summary>
/// A read model materialized by a projection.
/// </summary>
/// <param name="Value">The value of the read model.</param>
public record ProjectionReadModel(string Value);

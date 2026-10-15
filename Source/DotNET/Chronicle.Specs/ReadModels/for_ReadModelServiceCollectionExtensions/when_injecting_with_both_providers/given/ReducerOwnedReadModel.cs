// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_with_both_providers.given;

[ReadModel]
public record ReducerOwnedReadModel(string Id, string Value);

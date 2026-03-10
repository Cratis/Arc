// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;

namespace Chronicle;

[ReadModel]
[FromEvent<MyEvent>]
public record MyReadModel();

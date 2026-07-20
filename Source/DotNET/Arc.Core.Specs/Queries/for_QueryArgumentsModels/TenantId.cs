// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.for_QueryArgumentsModels;

public record TenantId(string Value) : ConceptAs<string>(Value);

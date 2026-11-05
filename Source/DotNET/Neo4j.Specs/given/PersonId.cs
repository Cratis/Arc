// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Neo4j.given;

public record PersonId(Guid Value) : ConceptAs<Guid>(Value);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Samples.Neo4j;

/// <summary>
/// The identity of a person, stored in the graph as the <c>personId</c> property of a <c>Person</c> node.
/// </summary>
/// <param name="Value">The inner value.</param>
public record PersonId(Guid Value) : ConceptAs<Guid>(Value);

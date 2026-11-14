// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Samples.Neo4j;

/// <summary>
/// The name of a group.
/// </summary>
/// <param name="Value">The inner value.</param>
public record GroupName(string Value) : ConceptAs<string>(Value);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// The exception that is thrown when a related clause of a graph query names a member that is not a relation of the view.
/// </summary>
/// <param name="view">The name of the view.</param>
/// <param name="member">The name of the member.</param>
public class UnknownRelationMember(string view, string member)
    : Exception($"The view '{view}' has no relation member named '{member}'.");

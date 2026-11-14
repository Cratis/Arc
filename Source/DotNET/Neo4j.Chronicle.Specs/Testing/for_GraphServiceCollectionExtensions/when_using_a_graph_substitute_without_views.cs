// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.Testing.for_GraphServiceCollectionExtensions;

public class when_using_a_graph_substitute_without_views : Specification
{
    GraphSubstitute _substitute;
    IReadOnlyList<GroupSummary> _groups;

    void Establish() => _substitute = new ServiceCollection().UseGraphSubstitute();

    async Task Because() => _groups = await _substitute.Returns(new GroupSummary(new GroupId(Guid.NewGuid()), null)).Nodes<GroupSummary>().ToList();

    [Fact] void should_discover_the_views_from_the_loaded_assemblies() => _groups.Count.ShouldEqual(1);
}

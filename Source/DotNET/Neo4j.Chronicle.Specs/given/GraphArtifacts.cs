// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Types;

namespace Cratis.Arc.Neo4j.Chronicle.given;

/// <summary>
/// Represents client artifacts that hold only the given graph elements and views, so discovery reads nothing else.
/// </summary>
/// <param name="elements">The node and edge types.</param>
/// <param name="views">The view types.</param>
public class GraphArtifacts(IEnumerable<Type> elements, IEnumerable<Type> views)
    : DefaultClientArtifactsProvider(Substitute.For<ICanProvideAssembliesForDiscovery>())
{
    /// <inheritdoc/>
    public override IEnumerable<Type> GraphElements => elements;

    /// <inheritdoc/>
    public override IEnumerable<Type> GraphProjections => [];

    /// <inheritdoc/>
    public override IEnumerable<Type> GraphViews => views;
}

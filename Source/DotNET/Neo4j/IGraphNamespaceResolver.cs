// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Defines a system that can resolve the name of the namespace whose graph is read.
/// </summary>
/// <remarks>
/// It must agree with the namespace Chronicle writes the graph for. When the Chronicle integration maps tenants to
/// namespaces in a way other than its default, register an implementation that does the same.
/// </remarks>
public interface IGraphNamespaceResolver
{
    /// <summary>
    /// Resolve the name of the namespace for the current tenant.
    /// </summary>
    /// <returns>The name of the namespace; <see cref="GraphDatabaseName.DefaultNamespace"/> for the default.</returns>
    string Resolve();
}

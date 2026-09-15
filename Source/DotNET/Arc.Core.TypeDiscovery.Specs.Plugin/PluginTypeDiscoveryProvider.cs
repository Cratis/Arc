// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Arc.TypeDiscovery.Plugin;

/// <summary>
/// Reports this assembly and <see cref="PluginMarker"/> to type discovery, in the shape the Fundamentals type
/// discovery generator emits.
/// </summary>
/// <remarks>
/// Written out by hand rather than left to the generator so the specification does not depend on which analyzers
/// happened to run over this project, and so the one type that carries the whole point of the arrangement is
/// visible in source.
/// </remarks>
public sealed class PluginTypeDiscoveryProvider : ICanProvideAssembliesForDiscovery
{
    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [typeof(PluginTypeDiscoveryProvider).Assembly];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => [typeof(IPluginMarker), typeof(PluginMarker)];

    /// <inheritdoc/>
    public void Initialize()
    {
    }
}

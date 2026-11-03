// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Determines which assemblies' model-bound artifacts a host serves.
/// </summary>
[IgnoreConvention]
internal sealed class ArtifactAssemblyFilter
{
    /// <summary>
    /// The compatibility filter used by direct provider construction.
    /// </summary>
    internal static readonly ArtifactAssemblyFilter IncludeAll = new([], _ => false);

    readonly HashSet<Assembly> _included;
    readonly Func<Assembly, bool> _requiresOptIn;
    readonly ConcurrentDictionary<Assembly, bool> _requirements = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactAssemblyFilter"/> class.
    /// </summary>
    /// <param name="options">The host's Arc options.</param>
    public ArtifactAssemblyFilter(IOptions<ArcOptions> options)
        : this(options.Value.Artifacts.IncludedAssemblies, assembly => assembly.IsDefined(typeof(ArtifactsRequireOptInAttribute), false))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactAssemblyFilter"/> class.
    /// </summary>
    /// <param name="included">The host's explicitly included assemblies.</param>
    /// <param name="requiresOptIn">Determines whether an assembly requires an opt-in.</param>
    internal ArtifactAssemblyFilter(IEnumerable<Assembly> included, Func<Assembly, bool> requiresOptIn)
    {
        _included = included.ToHashSet();
        _requiresOptIn = requiresOptIn;
    }

    /// <summary>
    /// Determines whether the host serves an artifact's assembly.
    /// </summary>
    /// <param name="artifactType">The artifact type.</param>
    /// <returns>Whether the artifact's assembly is unmarked or explicitly included.</returns>
    public bool Serves(Type artifactType) =>
        !_requirements.GetOrAdd(artifactType.Assembly, _requiresOptIn) || _included.Contains(artifactType.Assembly);
}

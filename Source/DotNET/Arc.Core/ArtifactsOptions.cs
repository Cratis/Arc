// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc;

/// <summary>
/// Represents host opt-ins for assemblies marked with <see cref="ArtifactsRequireOptInAttribute"/>.
/// </summary>
public class ArtifactsOptions
{
    readonly HashSet<Assembly> _includedAssemblies = [];

    /// <summary>
    /// Gets the assemblies explicitly included by the host.
    /// </summary>
    public IReadOnlyCollection<Assembly> IncludedAssemblies => _includedAssemblies.ToArray();

    /// <summary>
    /// Includes an assembly's model-bound commands and read-model queries in the host.
    /// </summary>
    /// <param name="assembly">The assembly to include.</param>
    /// <returns>The <see cref="ArtifactsOptions"/> for continuation.</returns>
    /// <exception cref="ArgumentNullException">The assembly is null.</exception>
    public ArtifactsOptions Include(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _includedAssemblies.Add(assembly);
        return this;
    }
}

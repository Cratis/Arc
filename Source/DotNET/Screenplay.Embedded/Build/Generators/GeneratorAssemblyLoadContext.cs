// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Resolves compiler identity before the runtime's default-context fallback can bind an analyzer.
/// </summary>
/// <param name="resolve">Resolves shared compiler assemblies and private analyzer dependencies.</param>
/// <remarks>
/// MSBuild loads tasks in private contexts. Resolving only through the Resolving event is too late when the
/// default context already contains another copy of Roslyn: generator interfaces then have a different identity.
/// </remarks>
public sealed class GeneratorAssemblyLoadContext(Func<AssemblyName, Assembly?> resolve)
    : AssemblyLoadContext("Cratis.Arc.Screenplay.Embedded.Analyzers", isCollectible: true)
{
    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName) => resolve(assemblyName);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Keeps collectible analyzer contexts available while their compilation or diagnostics are used.
/// </summary>
/// <remarks>
/// Keeping generator instances alone is insufficient: the load-context finalizer can start unloading and prevent
/// later dependency resolution even while those instances are still alive. Weak owners bound retention to the
/// compilation rather than the lifetime of a reused MSBuild process.
/// </remarks>
public static class GeneratorLifetimes
{
    static readonly ConditionalWeakTable<object, List<AnalyzerAssemblies>> _owners = new();

    /// <summary>
    /// Retains an analyzer loader for the lifetime of an object produced by it.
    /// </summary>
    /// <param name="owner">The generated compilation or diagnostic.</param>
    /// <param name="loader">The analyzer context that must remain available.</param>
    public static void Retain(object owner, AnalyzerAssemblies loader)
    {
        var loaders = _owners.GetOrCreateValue(owner);
        lock (loaders)
        {
            loaders.Add(loader);
        }
    }
}

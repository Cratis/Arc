// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Arc;

/// <summary>
/// Performs startup registration of generated metadata from project assemblies.
/// </summary>
static class GeneratedMetadataRegistration
{
    static int _hasLoadedProjectAssemblies;

    /// <summary>
    /// Ensures generated metadata has been registered for all project assemblies.
    /// </summary>
    public static void EnsureGeneratedMetadataRegistered()
    {
        EnsureProjectAssembliesLoaded();
    }

    static void EnsureProjectAssembliesLoaded()
    {
        if (Interlocked.Exchange(ref _hasLoadedProjectAssemblies, 1) == 1)
        {
            return;
        }

        var dependencyContext = DependencyContext.Default;
        if (dependencyContext is null)
        {
            return;
        }

        foreach (var runtimeLibrary in dependencyContext.RuntimeLibraries.Where(_ => _.Type == "project"))
        {
            try
            {
                _ = Assembly.Load(new AssemblyName(runtimeLibrary.Name));
            }
            catch
            {
                // Ignore failures and continue with assemblies that can be loaded.
            }
        }
    }
}

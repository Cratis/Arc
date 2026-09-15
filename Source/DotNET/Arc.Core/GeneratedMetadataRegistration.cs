// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Arc;

/// <summary>
/// Runs the module initializers that register compile-time generated metadata, for every project assembly in the
/// application's dependency context.
/// </summary>
/// <remarks>
/// Generators such as <c>QueryMetadataGenerator</c> emit a <c>[ModuleInitializer]</c> that registers what they
/// generated. The runtime runs a module initializer on first access to something in the module, not on load - so an
/// assembly nothing has touched yet has registered nothing, however many of them are sitting in the dependency
/// context.
/// </remarks>
static class GeneratedMetadataRegistration
{
    static int _hasRunProjectModuleInitializers;

    /// <summary>
    /// Ensures the module initializers of every project assembly have run, so the metadata they register is
    /// available.
    /// </summary>
    /// <remarks>
    /// Runs once per process; subsequent calls return immediately.
    /// </remarks>
    public static void EnsureGeneratedMetadataRegistered()
    {
        EnsureProjectModuleInitializersHaveRun();
    }

    static void EnsureProjectModuleInitializersHaveRun()
    {
        if (Interlocked.Exchange(ref _hasRunProjectModuleInitializers, 1) == 1)
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
                var assembly = Assembly.Load(new AssemblyName(runtimeLibrary.Name));

                // Loading is not enough on its own: the runtime defers the module initializer until something in the
                // module is first accessed, and merely holding an Assembly is not that. RunModuleConstructor forces
                // it, and the runtime still guarantees it runs at most once however it was reached.
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
            }
            catch
            {
                // Ignore failures and continue with assemblies that can be loaded and initialized.
            }
        }
    }
}

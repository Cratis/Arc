// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Runs the module initializers that register compile-time generated metadata, for every project assembly the
/// application references.
/// </summary>
/// <param name="loadAssembly">Loads a project assembly by name; used only for assemblies generated code cannot name.</param>
/// <param name="getDependencyContextProjectNames">Gets the names of the project libraries in the dependency context.</param>
/// <remarks>
/// <para>
/// Generators such as <c>QueryMetadataGenerator</c> and the Fundamentals type discovery generator emit a
/// <c>[ModuleInitializer]</c> that registers what they generated. The runtime runs a module initializer on first
/// access to something in the module, not on load - so an assembly nothing has touched yet has registered nothing.
/// </para>
/// <para>
/// An executable built with the Arc generators registers its project references through
/// <see cref="ProjectReferenceModuleInitializers"/>, naming a type in each so its module is reached without loading
/// anything by name. Only an application built without them falls back to the project libraries of the runtime
/// dependency context, which is not available in single-file applications and cannot be trimmed safely.
/// </para>
/// </remarks>
internal sealed class GeneratedMetadataRegistration(
    Func<AssemblyName, Assembly> loadAssembly,
    Func<IEnumerable<string>> getDependencyContextProjectNames)
{
    readonly object _lock = new();
    readonly Queue<GeneratedProjectReferenceModules> _pending = [];
    readonly List<SkippedProjectAssembly> _skipped = [];
    bool _hasGeneratedRegistrations;
    bool _hasRunDependencyContextFallback;

    /// <summary>
    /// Gets the process-wide instance generated code registers with.
    /// </summary>
    internal static GeneratedMetadataRegistration Default { get; } = CreateDefault();

    /// <summary>
    /// Gets the project assemblies that could not be loaded, and so registered nothing.
    /// </summary>
    internal IEnumerable<SkippedProjectAssembly> Skipped
    {
        get
        {
            lock (_lock)
            {
                return [.. _skipped];
            }
        }
    }

    /// <summary>
    /// Ensures the module initializers of every project assembly have run, so the metadata they register is
    /// available.
    /// </summary>
    /// <remarks>
    /// Runs each generated registration once; subsequent calls only run registrations that arrived since.
    /// </remarks>
    public static void EnsureGeneratedMetadataRegistered() => Default.EnsureRegistered();

    /// <summary>
    /// Logs, once, the project assemblies that could not be loaded while registering generated metadata.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> to resolve the <see cref="ILogger{TCategoryName}"/> from.</param>
    public static void LogSkippedProjectAssemblies(IServiceProvider services)
    {
        if (services.GetService<ILogger<GeneratedMetadataRegistration>>() is { } logger)
        {
            Default.LogSkipped(logger);
        }
    }

    /// <summary>
    /// Registers the project reference modules of an executable, as reported by its generated code.
    /// </summary>
    /// <param name="runModuleInitializers">Runs the module initializers of the project references generated code could name.</param>
    /// <param name="assembliesWithoutReachableTypes">Names of project references generated code could not name a type in.</param>
    internal void Register(Action runModuleInitializers, IEnumerable<string> assembliesWithoutReachableTypes)
    {
        lock (_lock)
        {
            _hasGeneratedRegistrations = true;
            _pending.Enqueue(new(runModuleInitializers, [.. assembliesWithoutReachableTypes]));
        }
    }

    /// <summary>
    /// Runs the module initializers of every project assembly not already run.
    /// </summary>
    /// <exception cref="TypeInitializationException">A module initializer threw.</exception>
    internal void EnsureRegistered()
    {
        // Module initializers run outside the lock: running one can reach an executable whose own generated module
        // initializer registers here, and holding the lock across arbitrary module code invites deadlocks.
        while (TryTakePending(out var registration))
        {
            registration.RunModuleInitializers();
            foreach (var assemblyName in registration.AssembliesWithoutReachableTypes)
            {
                LoadAndRunModuleInitializer(assemblyName);
            }
        }

        if (!TryTakeDependencyContextFallback())
        {
            return;
        }

        foreach (var projectName in getDependencyContextProjectNames())
        {
            LoadAndRunModuleInitializer(projectName);
        }
    }

    /// <summary>
    /// Logs the project assemblies that could not be loaded, and forgets them so they are logged once.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> to log with.</param>
    internal void LogSkipped(ILogger<GeneratedMetadataRegistration> logger)
    {
        SkippedProjectAssembly[] skipped;
        lock (_lock)
        {
            skipped = [.. _skipped];
            _skipped.Clear();
        }

        foreach (var assembly in skipped)
        {
            logger.ProjectAssemblyCouldNotBeLoaded(assembly.Name, assembly.Error);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Loading by name is reached only for project references generated code could not name, and for applications built without the Arc generators. A failed load is reported, not swallowed.")]
    [UnconditionalSuppressMessage("SingleFile", "IL3002", Justification = "The dependency context is consulted only for applications built without the Arc generators, where it is the only record of the project references.")]
    static GeneratedMetadataRegistration CreateDefault() => new(LoadAssemblyByName, GetDependencyContextProjectNames);

    [RequiresUnreferencedCode("Loads a project assembly by name, which trimming cannot see.")]
    static Assembly LoadAssemblyByName(AssemblyName name) => Assembly.Load(name);

    [RequiresAssemblyFiles("The dependency context is not available in single-file applications.")]
    static string[] GetDependencyContextProjectNames() =>
        DependencyContext.Default?.RuntimeLibraries
            .Where(_ => _.Type == "project")
            .Select(_ => _.Name)
            .ToArray() ?? [];

    bool TryTakePending([NotNullWhen(true)] out GeneratedProjectReferenceModules? registration)
    {
        lock (_lock)
        {
            return _pending.TryDequeue(out registration);
        }
    }

    bool TryTakeDependencyContextFallback()
    {
        lock (_lock)
        {
            if (_hasGeneratedRegistrations || _hasRunDependencyContextFallback)
            {
                return false;
            }

            _hasRunDependencyContextFallback = true;
            return true;
        }
    }

    void LoadAndRunModuleInitializer(string assemblyName)
    {
        Assembly assembly;
        try
        {
            assembly = loadAssembly(new AssemblyName(assemblyName));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            // The dependency context can name a project library that is not deployed, and a trimmed application can
            // have removed one nothing references. Either way it registers nothing; that is reported, not fatal.
            lock (_lock)
            {
                _skipped.Add(new(assemblyName, ex));
            }

            return;
        }

        // Loading is not enough on its own: the runtime defers the module initializer until something in the module
        // is first accessed, and merely holding an Assembly is not that. RunModuleConstructor forces it, and the
        // runtime still guarantees it runs at most once however it was reached.
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
    }

    sealed record GeneratedProjectReferenceModules(Action RunModuleInitializers, IEnumerable<string> AssembliesWithoutReachableTypes);
}

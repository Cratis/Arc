// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Runs the module initializers that register compile-time generated metadata, for every project assembly the
/// application references.
/// </summary>
/// <param name="loadAssembly">Loads a project assembly by name; used only for assemblies generated code cannot reach.</param>
/// <param name="getDependencyContextProjectNames">Gets the names of the project libraries in the dependency context.</param>
/// <param name="getEntryAssembly">Gets the entry assembly of the process, if any.</param>
/// <remarks>
/// <para>
/// Generators such as <c>QueryMetadataGenerator</c> and the Fundamentals type discovery generator emit a
/// <c>[ModuleInitializer]</c> that registers what they generated. The runtime runs a module initializer on first
/// access to something in the module, not on load - so an assembly nothing has touched yet has registered nothing.
/// </para>
/// <para>
/// An executable built with the Arc generators registers its project references through
/// <see cref="ProjectReferenceModuleInitializers"/>, naming a type in each so its module is reached without loading
/// anything by name. When the entry assembly registered that way, its project references are complete and the
/// runtime dependency context is not consulted. Otherwise - an application built without the Arc generators, or a
/// registration from an executable that is not the process root, such as a web application hosted by a test - the
/// project libraries of the dependency context are run as well. That fallback is not available in single-file
/// applications and cannot be trimmed safely; running a module initializer twice is a no-op.
/// </para>
/// <para>
/// Registration runs under an execution lock that is held for the whole of <see cref="EnsureRegistered"/>: a caller
/// arriving while another is registering waits for it, and then observes its failure if it failed. A registration
/// that fails is not counted as done: the failure is kept and thrown again by every later call, so no caller
/// returns before the registrations pending when it called have run, or proceeds after one has failed. The lock is
/// re-entrant, so a module initializer that reaches <see cref="EnsureRegistered"/> or registers again on the same
/// thread does not deadlock; it does not extend to a module initializer that waits on another thread which is
/// itself waiting in <see cref="EnsureRegistered"/>. Nor does it extend to the reverse: code running inside a
/// project's module initializer or a type initializer it depends on, on one thread, that builds an Arc host and so
/// waits for the lock, while another thread holds the lock and is running that same module initializer. The runtime
/// does not detect a deadlock between a lock and a class initializer, so both threads wait. Arc's generated module
/// initializers only register and never build a host; do not build an Arc host from a module or type initializer.
/// A registration made while another thread is registering is run by the next call, not by the one already running.
/// </para>
/// </remarks>
internal sealed class GeneratedMetadataRegistration(
    Func<AssemblyName, Assembly> loadAssembly,
    Func<IEnumerable<string>> getDependencyContextProjectNames,
    Func<Assembly?> getEntryAssembly)
{
    readonly object _lock = new();
    readonly object _runLock = new();
    readonly Queue<GeneratedProjectReferenceModules> _pending = [];
    readonly HashSet<Assembly> _registeringAssemblies = [];
    readonly List<SkippedProjectAssembly> _skipped = [];
    readonly HashSet<string> _skippedNames = new(StringComparer.OrdinalIgnoreCase);
    bool _hasRunDependencyContextFallback;
    ExceptionDispatchInfo? _failure;

    /// <summary>
    /// Gets the process-wide instance generated code registers with.
    /// </summary>
    internal static GeneratedMetadataRegistration Default { get; } = CreateDefault();

    /// <summary>
    /// Gets the project assemblies that could not be loaded, and so registered nothing, that have not been logged yet.
    /// Each assembly is reported once per registration, however many times and by whichever path it failed to load.
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
    /// Runs each generated registration once; subsequent calls only run registrations that arrived since. Callers
    /// are serialized: one that arrives while another is registering waits, and then observes its failure.
    /// </remarks>
    public static void EnsureGeneratedMetadataRegistered() => Default.EnsureRegistered();

    /// <summary>
    /// Logs the project assemblies that could not be loaded while registering generated metadata and have not been
    /// logged yet.
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
    /// <param name="registeringAssembly">The executable the generated code is in.</param>
    /// <param name="projectReferenceModules">Gets the module of each project reference generated code could name a type in, by assembly name.</param>
    /// <param name="assembliesWithoutReachableTypes">Names of project references generated code could not name a type in.</param>
    internal void Register(
        Assembly registeringAssembly,
        IEnumerable<KeyValuePair<string, Func<Module>>> projectReferenceModules,
        IEnumerable<string> assembliesWithoutReachableTypes)
    {
        lock (_lock)
        {
            _registeringAssemblies.Add(registeringAssembly);
            _pending.Enqueue(new([.. projectReferenceModules], [.. assembliesWithoutReachableTypes]));
        }
    }

    /// <summary>
    /// Runs the module initializers of every project assembly not already run.
    /// </summary>
    /// <exception cref="TypeInitializationException">A module initializer threw, now or on an earlier call.</exception>
    internal void EnsureRegistered()
    {
        // The execution lock spans the drain, the fallback and the failure capture, so no caller returns while
        // another is still registering, and a failure is visible to whoever was waiting. It is a Monitor, re-entrant
        // on the thread that holds it, because running a module initializer can reach an executable whose own
        // generated module initializer registers here. The short state lock is never held across module code.
        lock (_runLock)
        {
            lock (_lock)
            {
                _failure?.Throw();
            }

            try
            {
                while (TryTakePending(out var registration))
                {
                    foreach (var (assemblyName, getModule) in registration.Modules)
                    {
                        RunModuleInitializer(assemblyName, getModule);
                    }

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
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _failure ??= ExceptionDispatchInfo.Capture(ex);
                }

                throw;
            }
        }
    }

    /// <summary>
    /// Logs the project assemblies that could not be loaded, and forgets them so each is logged once.
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

    /// <summary>
    /// Creates the registration with the runtime fallback for applications built without the Arc generators.
    /// </summary>
    /// <returns>The <see cref="GeneratedMetadataRegistration"/>.</returns>
    /// <remarks>
    /// The fallback is not trim or single-file safe. Its warnings are left visible, and held by the trim/AOT ratchet
    /// baseline, rather than suppressed: an application whose entry assembly was built with the Arc generators reaches
    /// it only for a project reference whose type cannot be loaded.
    /// </remarks>
    static GeneratedMetadataRegistration CreateDefault() => new(LoadAssemblyByName, GetDependencyContextProjectNames, Assembly.GetEntryAssembly);

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
        var entryAssembly = getEntryAssembly();
        lock (_lock)
        {
            if (_hasRunDependencyContextFallback ||
                (entryAssembly is not null && _registeringAssemblies.Contains(entryAssembly)))
            {
                return false;
            }

            _hasRunDependencyContextFallback = true;
            return true;
        }
    }

    void RunModuleInitializer(string assemblyName, Func<Module> getModule)
    {
        Module module;
        try
        {
            // Generated code reaches the module through a type in it, each in its own method so one that cannot be
            // resolved - the assembly is not deployed, or the type's base type or interface lives in an assembly only
            // needed at compile time - cannot stop the others. A module initializer that throws surfaces as a
            // TypeInitializationException and is not caught.
            module = getModule();
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            LoadAndRunModuleInitializer(assemblyName);
            return;
        }

        // Generated code binds a type name in the executable's compilation after source generators have run, so a
        // same-named type there wins over the one chosen in the project reference and yields the wrong module.
        // Running the wrong module would register nothing for this assembly without any sign of it.
        if (!string.Equals(module.Assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
        {
            LoadAndRunModuleInitializer(assemblyName);
            return;
        }

        RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
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
                if (_skippedNames.Add(assemblyName))
                {
                    _skipped.Add(new(assemblyName, ex));
                }
            }

            return;
        }

        // Loading is not enough on its own: the runtime defers the module initializer until something in the module
        // is first accessed, and merely holding an Assembly is not that. RunModuleConstructor forces it, and the
        // runtime still guarantees it runs at most once however it was reached.
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
    }

    sealed record GeneratedProjectReferenceModules(
        IEnumerable<KeyValuePair<string, Func<Module>>> Modules,
        IEnumerable<string> AssembliesWithoutReachableTypes);
}

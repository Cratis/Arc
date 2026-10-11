// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Arc.ProxyGenerator;

public static partial class TypeExtensions
{
    /// <summary>
    /// Try to initialize the project assemblies.
    /// </summary>
    /// <param name="assemblyFile">Assembly file to start from.</param>
    /// <param name="message">Callback for outputting messages.</param>
    /// <param name="errorMessage">Callback for outputting error messages.</param>
    /// <returns>True if successful, false if not.</returns>
    internal static bool TryInitializeProjectAssemblies(string assemblyFile, Action<string> message, Action<string> errorMessage)
    {
        DetachProjectAssemblies(disposeMetadataLoadContext: true);
        _serverHandledCommandResponseValueTypeNames = [];
        _serverHandledCommandResponseValueAttributeTypeNames = [];
        _serverHandledCommandResponseValueEnumerableAttributeTypeNames = [];
        message($"  Gather all project referenced assemblies for {assemblyFile}");
        var assemblyFolder = Path.GetDirectoryName(assemblyFile)!;

        Assembly assembly;
        DependencyContext? dependencyContext;
        try
        {
            assembly = LoadRuntimeAssembly(assemblyFile);
            dependencyContext = DependencyContext.Load(assembly);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
        {
            errorMessage($"Could not load project assembly '{assemblyFile}': {ex.Message}");
            return false;
        }

        if (dependencyContext is null)
        {
            errorMessage($"Could not load dependency context for assembly '{assemblyFile}'");
            return false;
        }

        var root = RuntimeEnvironment.GetRuntimeDirectory();
        root = Path.GetDirectoryName(root)!;
        var aspNetCoreAppPath = GetAspNetCoreAppPath(root);

        message($"  Runtime assemblies: {root}");
        message($"  AspNetCoreApp assemblies: {aspNetCoreAppPath}");

        var runtimeAssemblies = Directory.GetFiles(root, "*.dll");
        var aspNetCoreAssemblies = Directory.GetFiles(aspNetCoreAppPath, "*.dll");
        var managedAppAssemblyPaths = GetManagedAssemblyPaths(dependencyContext.RuntimeLibraries, assemblyFolder, assemblyFile);
        var managedProjectAssemblyPaths = GetManagedAssemblyPaths(
            dependencyContext.RuntimeLibraries.Where(_ => _.Type.Equals("project")),
            assemblyFolder,
            assemblyFile);
        string[] paths = [.. runtimeAssemblies, .. aspNetCoreAssemblies, .. managedAppAssemblyPaths];

        if (FindFundamentalsAssemblyPath(dependencyContext) is { } fundamentalsPath)
        {
            paths = [.. paths, fundamentalsPath];
        }

        CreateMetadataLoadContext(paths.Distinct(new FileNameComparer()).ToArray());

        try
        {
            LoadProjectAssemblies(managedProjectAssemblyPaths, managedAppAssemblyPaths, message, errorMessage);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
        {
            errorMessage($"Could not load a managed project dependency for '{assemblyFile}': {ex.Message}");
            DisposeUnpublishedMetadataLoadContext();
            _serverHandledCommandResponseValueTypeNames = [];
            _serverHandledCommandResponseValueAttributeTypeNames = [];
            _serverHandledCommandResponseValueEnumerableAttributeTypeNames = [];
            return false;
        }
        catch
        {
            DisposeUnpublishedMetadataLoadContext();
            _serverHandledCommandResponseValueTypeNames = [];
            _serverHandledCommandResponseValueAttributeTypeNames = [];
            _serverHandledCommandResponseValueEnumerableAttributeTypeNames = [];
            throw;
        }

        return true;
    }

    /// <summary>
    /// Creates an owner for the currently initialized project assemblies.
    /// </summary>
    /// <returns>An owner that releases the metadata graph when disposed.</returns>
    internal static IDisposable OwnProjectAssemblies() => new ProjectAssembliesLifetime(_metadataLoadContext);

    /// <summary>
    /// Finds dependencies with a direct or indirect reference to Arc Core without loading their types.
    /// </summary>
    /// <param name="assemblies">The managed application dependencies.</param>
    /// <returns>Assemblies eligible to contain validators.</returns>
    internal static Assembly[] FindValidatorAssemblies(IEnumerable<Assembly> assemblies)
    {
        const string core = "Cratis.Arc.Core";
        var candidates = assemblies.ToArray();
        var dependents = BuildDependentsMap(candidates);

        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { core };
        var pending = new Queue<string>();
        pending.Enqueue(core);
        while (pending.TryDequeue(out var referenced))
        {
            EnqueueNewDependents(dependents, referenced, eligible, pending);
        }

        return [.. candidates.Where(_ => _.GetName().Name is { } name && name != core && eligible.Contains(name))];
    }

    static Dictionary<string, List<string>> BuildDependentsMap(Assembly[] candidates)
    {
        var dependents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in candidates)
        {
            var name = assembly.GetName().Name!;
            foreach (var referenceName in assembly.GetReferencedAssemblies().Select(_ => _.Name).OfType<string>())
            {
                if (!dependents.TryGetValue(referenceName, out var names))
                {
                    names = [];
                    dependents.Add(referenceName, names);
                }

                names.Add(name);
            }
        }

        return dependents;
    }

    static void EnqueueNewDependents(Dictionary<string, List<string>> dependents, string referenced, HashSet<string> eligible, Queue<string> pending)
    {
        if (!dependents.TryGetValue(referenced, out var names))
        {
            return;
        }

        foreach (var name in names.Where(eligible.Add))
        {
            pending.Enqueue(name);
        }
    }

    static Assembly LoadMetadataAssembly(string assemblyPath)
    {
        var normalizedPath = Path.GetFullPath(assemblyPath);
        var loadedAssembly = _metadataLoadContext!.GetAssemblies()
            .FirstOrDefault(_ => string.Equals(Path.GetFullPath(_.Location), normalizedPath, StringComparison.OrdinalIgnoreCase));
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        var assemblyName = AssemblyName.GetAssemblyName(normalizedPath);
        loadedAssembly = _metadataLoadContext.GetAssemblies()
            .FirstOrDefault(_ => string.Equals(_.FullName, assemblyName.FullName, StringComparison.Ordinal));

        return loadedAssembly ?? _metadataLoadContext.LoadFromAssemblyPath(normalizedPath);
    }

    static Assembly LoadRuntimeAssembly(string assemblyPath)
    {
        var normalizedPath = Path.GetFullPath(assemblyPath);
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(_ => !_.IsDynamic).ToArray();
        var loadedAssembly = loadedAssemblies.FirstOrDefault(_ =>
        {
            try
            {
                return !string.IsNullOrEmpty(_.Location) &&
                       string.Equals(Path.GetFullPath(_.Location), normalizedPath, StringComparison.OrdinalIgnoreCase);
            }
            catch (NotSupportedException)
            {
                return false;
            }
        });
        if (loadedAssembly is not null)
        {
            return loadedAssembly;
        }

        var assemblyName = AssemblyName.GetAssemblyName(normalizedPath);
        loadedAssembly = loadedAssemblies.FirstOrDefault(_ =>
        {
            try
            {
                return string.IsNullOrEmpty(_.Location) && string.Equals(_.FullName, assemblyName.FullName, StringComparison.Ordinal);
            }
            catch (NotSupportedException)
            {
                return string.Equals(_.FullName, assemblyName.FullName, StringComparison.Ordinal);
            }
        });

        return loadedAssembly ?? Assembly.LoadFile(normalizedPath);
    }

    static void DetachProjectAssemblies(bool disposeMetadataLoadContext = false)
    {
        var metadataLoadContext = _metadataLoadContext;
        Assemblies = [];
        ValidatorAssemblies = [];
        ValidatorTypes.SetWarningLogger(_ => { });
        _assembliesByName.Clear();
        _serverHandledCommandResponseValueTypeNames = null;
        _serverHandledCommandResponseValueAttributeTypeNames = null;
        _serverHandledCommandResponseValueEnumerableAttributeTypeNames = null;
        ResetWellKnownTypes();

        if (_assemblyResolvingHandler is not null)
        {
            AssemblyLoadContext.Default.Resolving -= _assemblyResolvingHandler;
            _assemblyResolvingHandler = null;
        }

        _metadataLoadContext = null;
        _assemblyResolver = null;
        if (disposeMetadataLoadContext)
        {
            metadataLoadContext?.Dispose();
        }
    }

    static void ResetWellKnownTypes()
    {
        _conceptType = typeof(NoConcept);
        _controllerBaseType = typeof(object);
        _taskType = typeof(Task);
        _voidType = typeof(void);
    }

    static void DisposeUnpublishedMetadataLoadContext()
    {
        DetachProjectAssemblies(disposeMetadataLoadContext: true);
    }

    static void InitializeWellKnownTypes()
    {
        _taskType = GetMetadataType(typeof(Task));
        _voidType = GetMetadataType(typeof(void));

        try
        {
            var fundamentals = _metadataLoadContext.LoadFromAssemblyName("Cratis.Fundamentals");
            _conceptType = fundamentals.GetType("Cratis.Concepts.ConceptAs`1")!;
        }
        catch
        {
            Console.WriteLine("  Could not load Cratis.Fundamentals, concepts will not be supported");
        }

        var aspNetCore = _metadataLoadContext.LoadFromAssemblyName("Microsoft.AspNetCore.Mvc.Core");
        _controllerBaseType = aspNetCore.GetType("Microsoft.AspNetCore.Mvc.ControllerBase")!;
    }

    static Type GetMetadataType(Type runtimeType) =>
        _metadataLoadContext!
            .LoadFromAssemblyName(runtimeType.Assembly.GetName())
            .GetType(runtimeType.FullName!)!;

    static void CreateMetadataLoadContext(string[] assemblyPaths)
    {
        _assemblyResolver = new PathAssemblyResolver(assemblyPaths);
#pragma warning disable CA2000 // Dispose objects before losing scope
        _metadataLoadContext = new MetadataLoadContext(_assemblyResolver);
#pragma warning restore CA2000 // Dispose objects before losing scope

        _assemblyResolvingHandler = (_, name) =>
        {
            // Caching the metadata-only assembly is the point here - IsAssignableTo<T> needs it to translate a
            // runtime type into the metadata world. Handing it back is not: the default context accepts only runtime
            // assemblies and throws "Resolved assembly must be a runtime Assembly object" on anything else, which
            // would turn any genuine resolution miss into a hard failure.
            if (_assemblyResolver.Resolve(_metadataLoadContext, name) is { } resolved)
            {
                _assembliesByName[name.Name!] = resolved;
            }

            return null;
        };
        AssemblyLoadContext.Default.Resolving += _assemblyResolvingHandler;
    }

    static string GetAspNetCoreAppPath(string runtimeRoot)
    {
        var version = Path.GetFileName(runtimeRoot)!;
        var framework = Directory.GetParent(runtimeRoot)!;
        var shared = Directory.GetParent(framework.FullName)!;
        return Path.Combine(shared.FullName, "Microsoft.AspNetCore.App", version);
    }

    static string[] GetManagedAssemblyPaths(IEnumerable<RuntimeLibrary> libraries, string assemblyFolder, string assemblyFile) =>
        libraries
            .SelectMany(_ => _.RuntimeAssemblyGroups)
            .SelectMany(_ => _.AssetPaths)
            .Select(Path.GetFileName)
            .Where(_ => !string.IsNullOrEmpty(_))
            .Select(_ => Path.Join(assemblyFolder, _))
            .Append(assemblyFile)
            .Where(File.Exists)
            .Distinct(new FileNameComparer())
            .ToArray();

    static string? FindFundamentalsAssemblyPath(DependencyContext dependencyContext)
    {
        var fundamentalsPackage = dependencyContext.RuntimeLibraries.SingleOrDefault(_ => _.Type == "package" && _.Name == "Cratis.Fundamentals");
        if (fundamentalsPackage is null)
        {
            return null;
        }

        var assetPaths = fundamentalsPackage.RuntimeAssemblyGroups
            .SelectMany(_ => _.AssetPaths)
            .ToArray();

        var nugetCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages");

        var fullPath = Path.Combine(nugetCachePath, fundamentalsPackage.Path!, assetPaths[0]);
        return File.Exists(fullPath) ? fullPath : null;
    }

    static void LoadProjectAssemblies(
        string[] managedProjectAssemblyPaths,
        string[] managedAppAssemblyPaths,
        Action<string> message,
        Action<string> errorMessage)
    {
        // Project references can represent analyzer-only or package-only dependencies with no runtime assembly,
        // and their logical package ID can differ from the assembly file name. Load the actual existing runtime
        // assets declared by project libraries instead of synthesizing file names from dependency identities.
        Assemblies = [.. managedProjectAssemblyPaths
                                        .Select(LoadMetadataAssembly)
                                        .Distinct()];
        ValidatorAssemblies = FindValidatorAssemblies(managedAppAssemblyPaths.Select(LoadMetadataAssembly).Distinct());
        ValidatorTypes.SetWarningLogger(message);

        var commandResponseValueHandlerContractsAssembly = managedAppAssemblyPaths
            .Where(_ => Path.GetFileNameWithoutExtension(_) == CommandResponseValueHandlerContractsAssemblyName)
            .Select(LoadMetadataAssembly)
            .SingleOrDefault();
        _serverHandledCommandResponseValueTypeNames = FindServerHandledCommandResponseValueTypeNames(
            managedAppAssemblyPaths.Select(LoadMetadataAssembly), commandResponseValueHandlerContractsAssembly, errorMessage);
        _serverHandledCommandResponseValueAttributeTypeNames = FindServerHandledCommandResponseValueTypeNames(
            managedAppAssemblyPaths.Select(LoadMetadataAssembly), commandResponseValueHandlerContractsAssembly, errorMessage, AttributedCommandResponseValueHandlerTypeName);
        _serverHandledCommandResponseValueEnumerableAttributeTypeNames = FindServerHandledCommandResponseValueTypeNames(
            managedAppAssemblyPaths.Select(LoadMetadataAssembly), commandResponseValueHandlerContractsAssembly, errorMessage, AttributedEnumerableCommandResponseValueHandlerTypeName);

        foreach (var loadedAssembly in _metadataLoadContext!.GetAssemblies())
        {
            _assembliesByName[loadedAssembly.GetName().Name!] = loadedAssembly;
        }

        InitializeWellKnownTypes();
    }

    sealed class ProjectAssembliesLifetime(MetadataLoadContext? ownedMetadataLoadContext) : IDisposable
    {
        bool _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (ReferenceEquals(_metadataLoadContext, ownedMetadataLoadContext))
            {
                DetachProjectAssemblies(disposeMetadataLoadContext: true);
            }
            else
            {
                ownedMetadataLoadContext?.Dispose();
            }

            _disposed = true;
        }
    }
}

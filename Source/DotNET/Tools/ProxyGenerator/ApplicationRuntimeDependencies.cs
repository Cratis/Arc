// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Resolves the target application's own runtime dependencies for the code the generator executes.
/// </summary>
/// <remarks>
/// <para>
/// Reading FluentValidation rules runs validator constructors from the target application, inside the generator's
/// process. The process resolves assemblies from the generator's dependency manifest, not the application's, so an
/// application dependency is found only when the request happens to come from an assembly loaded from the
/// application's output folder. Requests without a requesting assembly - a load by name through a load context, as
/// serializers and type registries do - or from another load context are not.
/// </para>
/// <para>
/// Used as a last-resort <see cref="AppDomain.AssemblyResolve"/> handler, this answers those requests from the
/// application's <c>deps.json</c>, falling back to the application's output folder. It is consulted only after every
/// other resolution has missed, so nothing that resolves today changes. Assemblies load into the default context,
/// alongside the executable validator assemblies, so shared types keep a single identity.
/// </para>
/// </remarks>
internal sealed class ApplicationRuntimeDependencies
{
    readonly string _applicationFolder;
    readonly AssemblyDependencyResolver? _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRuntimeDependencies"/> class.
    /// </summary>
    /// <param name="assemblyFile">The application's main assembly, next to its <c>deps.json</c>.</param>
    public ApplicationRuntimeDependencies(string assemblyFile)
    {
        var fullPath = Path.GetFullPath(assemblyFile);
        _applicationFolder = Path.GetDirectoryName(fullPath)!;
        _resolver = CreateResolver(fullPath);
    }

    /// <summary>
    /// Resolves an assembly the process could not resolve on its own.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">The resolution request.</param>
    /// <returns>The application's copy of the assembly, or <see langword="null"/> when the application has none.</returns>
    public Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        var path = PathFor(new AssemblyName(args.Name));
        if (path is null)
        {
            return null;
        }

        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
        catch (Exception exception) when (exception is FileLoadException or BadImageFormatException)
        {
            // Another copy already owns the name; leave the original resolution failure to surface.
            return null;
        }
    }

    static AssemblyDependencyResolver? CreateResolver(string assemblyFile)
    {
        try
        {
            return new AssemblyDependencyResolver(assemblyFile);
        }
        catch (InvalidOperationException)
        {
            // No readable dependency manifest; the output folder is still probed.
            return null;
        }
    }

    string? PathFor(AssemblyName name)
    {
        if (_resolver?.ResolveAssemblyToPath(name) is { } resolved && File.Exists(resolved))
        {
            return resolved;
        }

        if (string.IsNullOrEmpty(name.Name))
        {
            return null;
        }

        var probed = Path.Combine(_applicationFolder, $"{name.Name}.dll");
        return File.Exists(probed) ? probed : null;
    }
}

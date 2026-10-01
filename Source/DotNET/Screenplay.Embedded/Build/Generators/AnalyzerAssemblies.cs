// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Loads the assemblies the analyzers of a project live in.
/// </summary>
/// <remarks>
/// An analyzer is loaded beside the task rather than into it. Its own dependencies - the ones shipped next to it in
/// its package - are resolved from the directory it was found in, so a generator that ships a private copy of a
/// library the task also uses keeps the copy it was built against.
/// <para>
/// The compiler assemblies are the exception and are deliberately not isolated. A generator implements
/// <see cref="ISourceGenerator"/> and is handed a compilation; if its Roslyn came from somewhere else those types
/// would not be the same types, and every generator would fail to load for a reason that reads like a version
/// conflict. The context is collectible: generator and diagnostic objects keep it alive while they are used,
/// and it can be reclaimed after the compilation finishes instead of accumulating in reused MSBuild nodes.
/// </para>
/// </remarks>
public sealed class AnalyzerAssemblies : IAnalyzerAssemblyLoader
{
    static readonly StringComparer _paths = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    readonly ConcurrentDictionary<string, string> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, Assembly> _assemblies;
    readonly AssemblyLoadContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzerAssemblies"/> class.
    /// </summary>
    public AnalyzerAssemblies()
    {
        _assemblies = new(_paths);
        _context = new GeneratorAssemblyLoadContext(Resolve);
    }

    /// <inheritdoc/>
    public void AddDependencyLocation(string fullPath)
    {
        var path = Path.GetFullPath(fullPath);

        Remember(path);

        foreach (var sibling in Siblings(path))
        {
            Remember(sibling);
        }
    }

    /// <inheritdoc/>
    public Assembly LoadFromPath(string fullPath)
    {
        var path = Path.GetFullPath(fullPath);
        AddDependencyLocation(path);

        return _assemblies.GetOrAdd(path, Load);
    }

    /// <summary>
    /// Determines whether an assembly has to be the one the task already runs on.
    /// </summary>
    /// <param name="name">The simple name of the assembly.</param>
    /// <returns>True when the assembly must not be isolated, false otherwise.</returns>
    /// <remarks>
    /// The runtime and the compiler are shared because their types cross the boundary. Everything else an analyzer
    /// brings with it is its own business.
    /// </remarks>
    static bool MustBeShared(string name) =>
        name.Equals("Microsoft.CodeAnalysis", StringComparison.Ordinal) ||
        name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) ||
        name.Equals("Microsoft.CSharp", StringComparison.Ordinal) ||
        name.Equals("Microsoft.VisualBasic", StringComparison.Ordinal) ||
        name.Equals("System", StringComparison.Ordinal) ||
        name.StartsWith("System.", StringComparison.Ordinal) ||
        name.StartsWith("Microsoft.Win32.", StringComparison.Ordinal) ||
        name.Equals("netstandard", StringComparison.Ordinal) ||
        name.Equals("mscorlib", StringComparison.Ordinal);

    /// <summary>
    /// Gets the assemblies sitting beside an analyzer.
    /// </summary>
    /// <param name="path">The path of the analyzer.</param>
    /// <returns>The path of every assembly in the same directory.</returns>
    static IEnumerable<string> Siblings(string path)
    {
        var directory = Path.GetDirectoryName(path);

        return string.IsNullOrEmpty(directory) || !Directory.Exists(directory)
            ? []
            : Directory.EnumerateFiles(directory, "*.dll").Order(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the assembly the task already runs on, when it has to be shared.
    /// </summary>
    /// <param name="name">The name of the assembly wanted.</param>
    /// <returns>The <see cref="Assembly"/>, or null when it is not one that has to be shared.</returns>
    /// <remarks>
    /// It is asked for by simple name on purpose. A generator built against an older compiler asks for an older
    /// version, and being handed the running one is exactly what the C# compiler does for it.
    /// </remarks>
    static Assembly? Shared(AssemblyName name)
    {
        if (name.Name is not { } simple || !MustBeShared(simple))
        {
            return null;
        }

        if (string.Equals(simple, "Microsoft.CodeAnalysis", StringComparison.Ordinal))
        {
            return typeof(Compilation).Assembly;
        }

        if (string.Equals(simple, "Microsoft.CodeAnalysis.CSharp", StringComparison.Ordinal))
        {
            return typeof(CSharpCompilation).Assembly;
        }

        if (string.Equals(simple, "System.Collections.Immutable", StringComparison.Ordinal))
        {
            return typeof(ImmutableArray<>).Assembly;
        }

        if (string.Equals(simple, "System.Reflection.Metadata", StringComparison.Ordinal))
        {
            return typeof(MetadataReader).Assembly;
        }

        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(simple));
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Loads an analyzer assembly.
    /// </summary>
    /// <param name="path">The path of the assembly.</param>
    /// <returns>The <see cref="Assembly"/>.</returns>
    Assembly Load(string path) => Shared(AssemblyName.GetAssemblyName(path)) ?? _context.LoadFromAssemblyPath(path);

    /// <summary>
    /// Resolves a dependency of an analyzer.
    /// </summary>
    /// <param name="name">The name of the assembly wanted.</param>
    /// <returns>The <see cref="Assembly"/>, or null when nothing beside the analyzers provides it.</returns>
    Assembly? Resolve(AssemblyName name) =>
        Shared(name) ??
        (name.Name is { } simple && _dependencies.TryGetValue(simple, out var path)
            ? _assemblies.GetOrAdd(path, Load)
            : null);

    /// <summary>
    /// Remembers where an assembly can be loaded from.
    /// </summary>
    /// <param name="path">The path of the assembly.</param>
    /// <remarks>
    /// The first path wins, which makes the outcome follow the order the analyzers were stated in rather than the
    /// order a directory happened to be enumerated in.
    /// </remarks>
    void Remember(string path) => _dependencies.TryAdd(Path.GetFileNameWithoutExtension(path), path);
}

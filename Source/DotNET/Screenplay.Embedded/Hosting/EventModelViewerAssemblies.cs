// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Finds the assemblies the automatically mapped explorer serves documents from.
/// </summary>
/// <remarks>
/// <para>
/// The application's own assemblies are the ones that embed documents, and the application states which they are
/// by referencing them. So the entry assembly and the assemblies it references are what is served - no types are
/// loaded, nothing is discovered by convention, and an assembly that happens to be loaded without being
/// referenced is never served.
/// </para>
/// <para>
/// The framework's and Cratis's own assemblies are left out by name as well. They embed no catalog and would
/// contribute nothing, but naming them explicitly means a future Cratis assembly that does embed one cannot
/// start showing up in an application's explorer.
/// </para>
/// </remarks>
static class EventModelViewerAssemblies
{
    static readonly string[] _excludedPrefixes =
    [
        "System",
        "Microsoft",
        "netstandard",
        "mscorlib",
        "WindowsBase",
        "Cratis."
    ];

    /// <summary>
    /// Gets the assemblies to serve, in a stable order.
    /// </summary>
    /// <param name="entryAssembly">The entry assembly of the process, or null when the process has none.</param>
    /// <param name="loadedAssemblies">The assemblies loaded in the process.</param>
    /// <param name="configuredAssemblies">The assemblies the host named itself; these are served as given.</param>
    /// <returns>The assemblies to serve embedded documents from.</returns>
    internal static IReadOnlyList<Assembly> For(
        Assembly? entryAssembly,
        IEnumerable<Assembly> loadedAssemblies,
        IEnumerable<Assembly> configuredAssemblies)
    {
        var assemblies = new List<Assembly>();
        if (entryAssembly is not null)
        {
            assemblies.Add(entryAssembly);
            assemblies.AddRange(ReferencedBy(entryAssembly, loadedAssemblies));
        }

        assemblies.AddRange(configuredAssemblies);
        return [.. assemblies.Distinct()];
    }

    /// <summary>
    /// Gets whether an assembly name is the framework's or Cratis's own.
    /// </summary>
    /// <param name="name">The simple name of the assembly.</param>
    /// <returns>True when the assembly is never served automatically.</returns>
    internal static bool IsFrameworkOrTooling(string name) =>
        string.Equals(name, "Cratis", StringComparison.Ordinal) ||
        Array.Exists(_excludedPrefixes, prefix => name.StartsWith(prefix, StringComparison.Ordinal));

    static IEnumerable<Assembly> ReferencedBy(Assembly entryAssembly, IEnumerable<Assembly> loadedAssemblies)
    {
        var referenced = entryAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrEmpty(name) && !IsFrameworkOrTooling(name))
            .ToHashSet(StringComparer.Ordinal);

        return loadedAssemblies
            .Where(assembly => !assembly.IsDynamic && referenced.Contains(assembly.GetName().Name ?? string.Empty))
            .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal);
    }
}

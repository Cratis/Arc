// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Finds and reports proxy names that collide within a generated TypeScript module.
/// </summary>
internal static class ProxyNameCollisions
{
    /// <summary>
    /// Finds descriptors with the same proxy name and output module.
    /// </summary>
    /// <param name="descriptors">The command and query descriptors to check.</param>
    /// <param name="segmentsToSkip">The number of namespace segments to skip.</param>
    /// <param name="sourceFileMap">The source file map, or null to use one file per artifact.</param>
    /// <returns>The collisions ordered by module and proxy name.</returns>
    internal static IReadOnlyList<ProxyNameCollision> Find(IEnumerable<IDescriptor> descriptors, int segmentsToSkip, IReadOnlyDictionary<string, string>? sourceFileMap) =>
        descriptors
            .GroupBy(descriptor => (
                Module: Path.Join(descriptor.Type.ResolveTargetPath(segmentsToSkip), descriptor.Type.GetGeneratedModuleBaseName(sourceFileMap, descriptor.Name)),
                descriptor.Name))
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.Module, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Name, StringComparer.Ordinal)
            .Select(group => new ProxyNameCollision(group.Key.Module, group.Key.Name, group.ToArray()))
            .ToArray();

    /// <summary>
    /// Reports colliding proxy names and their sources.
    /// </summary>
    /// <param name="collisions">The collisions to report.</param>
    /// <param name="errorMessage">The error message callback.</param>
    internal static void Report(IEnumerable<ProxyNameCollision> collisions, Action<string> errorMessage)
    {
        foreach (var collision in collisions)
        {
            errorMessage($"ERROR: More than one proxy named '{collision.Name}' would be generated into the TypeScript module '{collision.Module}':");
            foreach (var descriptor in collision.Descriptors)
            {
                errorMessage(descriptor is QueryDescriptor query
                    ? $"  {query.Type.FullName}.{query.Method.Name}"
                    : $"  {descriptor.Type.FullName}");
            }
            errorMessage("Rename one of them. The proxy class and its conventional route are both named after the C# query method or command, so the server would also serve only one of them.");
        }
    }
}

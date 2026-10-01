// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Resolves which documents an application is embedded as.
/// </summary>
/// <remarks>
/// The hierarchy is read off the namespaces relative to the root namespace, because that is the division the
/// application already has. The first segment beneath the root is a module when there are namespaces between it and
/// the slices it holds, and a feature when the only thing beneath it is slices - so an application that groups its
/// features directly under its root namespace is not given a module it never wrote.
/// <para>
/// Nothing here reads the file system, the project or the order the model happened to arrive in. The same model and
/// the same root namespace always yield the same scopes, in the same order, parents before children.
/// </para>
/// </remarks>
public static class DocumentScopes
{
    /// <summary>
    /// Gets the scope of the document holding everything the assembly declares.
    /// </summary>
    /// <param name="options">The options the documents are generated with.</param>
    /// <returns>The <see cref="DocumentScope"/>.</returns>
    /// <remarks>
    /// The root document skips every segment of the root namespace but the last, which leaves that last segment as
    /// the module of the document - the name the application already goes by - and the segments below it as the
    /// features.
    /// </remarks>
    public static DocumentScope Root(EmbeddedDocumentOptions options)
    {
        var resolved = options.Resolve();
        var segments = Namespaces.Segments(resolved.RootNamespace);

        return new(
            resolved.RootNamespace!,
            resolved.AssemblyName!,
            EmbeddedDocumentKind.Assembly,
            null,
            Namespaces.LastSegment(resolved.RootNamespace, resolved.AssemblyName!),
            Math.Max(segments.Count - 1, 0));
    }

    /// <summary>
    /// Gets the scope of every document an application is embedded as, parents before children.
    /// </summary>
    /// <param name="model">The model the documents are generated from.</param>
    /// <param name="options">The options the documents are generated with.</param>
    /// <returns>The scopes.</returns>
    public static IReadOnlyList<DocumentScope> Of(ApplicationModel model, EmbeddedDocumentOptions options) =>
        [Root(options), .. Below(model, options)];

    /// <summary>
    /// Gets the scope of every document beneath the assembly document, parents before children.
    /// </summary>
    /// <param name="model">The model the documents are generated from.</param>
    /// <param name="options">The options the documents are generated with.</param>
    /// <returns>The scopes.</returns>
    /// <remarks>
    /// A slice sitting directly in the root namespace belongs to no module and to no feature, so it is described by
    /// the assembly document and by nothing else. The same holds for a slice outside the root namespace entirely -
    /// the assembly declares it, so the assembly document holds it, but there is no place for it in a hierarchy
    /// that is relative to a root it is not under.
    /// </remarks>
    public static IReadOnlyList<DocumentScope> Below(ApplicationModel model, EmbeddedDocumentOptions options)
    {
        var resolved = options.Resolve();
        var root = resolved.RootNamespace!;
        var rootSegments = Namespaces.Segments(root);
        var rootModule = Namespaces.LastSegment(root, resolved.AssemblyName!);
        var sliceNamespaces = model.Slices.Select(_ => _.Namespace).ToHashSet(StringComparer.Ordinal);
        var placed = model.Slices
            .Select(_ => Namespaces.Relative(_.Namespace, root))
            .Where(_ => _?.Count >= 2)
            .Select(_ => _!)
            .ToList();

        var scopes = new List<DocumentScope>();

        foreach (var group in placed
            .GroupBy(_ => _[0], StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var @namespace = $"{root}{Namespaces.Separator}{group.Key}";
            var hasFeaturesBeneath = group.Any(slice => Enumerable.Range(2, Math.Max(slice.Count - 2, 0))
                .Any(depth => !sliceNamespaces.Contains($"{root}{Namespaces.Separator}{Namespaces.Join(slice.Take(depth))}")));

            if (!hasFeaturesBeneath)
            {
                scopes.Add(new(
                    @namespace,
                    group.Key,
                    EmbeddedDocumentKind.Feature,
                    root,
                    rootModule,
                    Math.Max(rootSegments.Count - 1, 0)));
                continue;
            }

            scopes.Add(new(@namespace, group.Key, EmbeddedDocumentKind.Module, root, group.Key, rootSegments.Count));
            scopes.AddRange(FeaturesOf(group, @namespace, group.Key, rootSegments.Count, sliceNamespaces));
        }

        return scopes;
    }

    /// <summary>
    /// Gets the scope of every feature beneath a module, outer features before the ones nested in them.
    /// </summary>
    /// <param name="slices">The relative segments of every slice within the module.</param>
    /// <param name="moduleNamespace">The full namespace of the module.</param>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments the module's features are resolved after.</param>
    /// <param name="sliceNamespaces">The namespaces already identified as slices, not features.</param>
    /// <returns>The scopes.</returns>
    static IEnumerable<DocumentScope> FeaturesOf(
        IEnumerable<IReadOnlyList<string>> slices,
        string moduleNamespace,
        string moduleName,
        int segmentsToSkip,
        HashSet<string> sliceNamespaces)
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            for (var depth = 1; depth <= slice.Count - 2; depth++)
            {
                var path = Namespaces.Join(slice.Skip(1).Take(depth));
                if (!sliceNamespaces.Contains($"{moduleNamespace}{Namespaces.Separator}{path}"))
                {
                    paths.Add(path);
                }
            }
        }

        return paths.Select(path =>
        {
            var segments = Namespaces.Segments(path);
            var ancestor = Enumerable.Range(1, Math.Max(segments.Count - 1, 0))
                .Reverse().Select(depth => Namespaces.Join(segments.Take(depth)))
                .FirstOrDefault(paths.Contains);
            var parent = ancestor is null ? moduleNamespace : $"{moduleNamespace}{Namespaces.Separator}{ancestor}";

            return new DocumentScope(
                $"{moduleNamespace}{Namespaces.Separator}{path}",
                segments[^1],
                EmbeddedDocumentKind.Feature,
                parent,
                moduleName,
                segmentsToSkip);
        });
    }
}

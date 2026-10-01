// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Screenplay.Embedded.Generation;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Writes the generated documents and catalog where the compiler picks them up from.
/// </summary>
/// <remarks>
/// The files live in the intermediate output of the project being built, because they are compiler input rather
/// than something a developer edits - generation never writes into the source tree. A file whose content has not
/// changed is left alone so that its timestamp keeps saying when the document last really changed, and a file left
/// behind by an earlier build that no longer describes anything is removed rather than silently kept.
/// </remarks>
public static class GeneratedFiles
{
    /// <summary>
    /// The extension the generated catalog is written with.
    /// </summary>
    public const string CatalogFileName = "catalog.json";

    /// <summary>
    /// Writes everything a generation produced.
    /// </summary>
    /// <param name="directory">The directory to write into.</param>
    /// <param name="generation">The generation to write.</param>
    /// <returns>Every file to embed, ordered the way the catalog orders the documents.</returns>
    public static IReadOnlyList<EmbeddedResourceFile> Write(string directory, EmbeddedDocumentGeneration generation)
    {
        Directory.CreateDirectory(directory);

        var files = new List<EmbeddedResourceFile>();

        foreach (var document in generation.Documents)
        {
            var path = Path.Combine(directory, document.Document.ResourceName);
            WriteIfChanged(path, document.Source);
            files.Add(new(path, document.Document.ResourceName));
        }

        var catalog = Path.Combine(directory, CatalogFileName);
        WriteIfChanged(catalog, generation.Catalog.Serialize());
        files.Add(new(catalog, EmbeddedResourceNames.Catalog));

        Prune(directory, files);

        return files;
    }

    /// <summary>
    /// Writes a file unless it already holds exactly this content.
    /// </summary>
    /// <param name="path">The path to write to.</param>
    /// <param name="content">The content to write.</param>
    static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
        {
            return;
        }

        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    /// <summary>
    /// Removes everything an earlier generation left in the directory that this one did not produce.
    /// </summary>
    /// <param name="directory">The directory to prune.</param>
    /// <param name="files">The files this generation produced.</param>
    static void Prune(string directory, IEnumerable<EmbeddedResourceFile> files)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var written = files.Select(_ => _.Path).ToHashSet(comparer);

        foreach (var stale in Directory
            .EnumerateFiles(directory)
            .Where(_ => Path.GetFileName(_).StartsWith(EmbeddedResourceNames.Documents, StringComparison.Ordinal) &&
                _.EndsWith(EmbeddedResourceNames.DocumentExtension, StringComparison.Ordinal) && !written.Contains(_)))
        {
            File.Delete(stale);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

public static partial class Generator
{
    static void CollectProducedFiles(ICollection<string>? producedFiles, IDictionary<string, GeneratedFileMetadata> generatedFiles, bool skipIndexGeneration, string outputPath)
    {
        if (producedFiles is null) return;
        foreach (var path in generatedFiles.Keys) producedFiles.Add(path);
        if (skipIndexGeneration || !Directory.Exists(outputPath)) return;
        var indexPaths = generatedFiles.Keys.Select(Path.GetDirectoryName).OfType<string>()
            .Distinct(StringComparer.Ordinal).Select(directory => Path.Combine(directory, "index.ts"));
        foreach (var path in indexPaths.Where(File.Exists))
        {
            producedFiles.Add(Path.GetFullPath(path));
        }
    }
}

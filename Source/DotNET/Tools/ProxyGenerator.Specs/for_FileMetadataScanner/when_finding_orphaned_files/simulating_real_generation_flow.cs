// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class simulating_real_generation_flow : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _generatedFiles = [];

        var targetPath = _tempDir;
        var path = "commands";
        var fileName = "CreateOrder.ts";

        var fullPath = Path.Join(targetPath, path, fileName);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var directory = Path.GetDirectoryName(normalizedFullPath)!;
        Directory.CreateDirectory(directory);

        var metadata = new GeneratedFileMetadata("MyApp.Commands.CreateOrder", DateTime.UtcNow);
        var content = $"{metadata.ToCommentLine()}\nexport class CreateOrder {{}}";
        File.WriteAllText(normalizedFullPath, content);

        _generatedFiles[normalizedFullPath] = metadata;
    }

    void Because() => _orphanedFiles = FileMetadataScanner.FindOrphanedFiles(_tempDir, _generatedFiles);

    [Fact] void should_not_find_any_orphaned_files() => _orphanedFiles.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class with_file_just_generated_in_same_run : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;
    string _justGeneratedFilePath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _justGeneratedFilePath = Path.Combine(_tempDir, "JustGenerated.ts");
        var metadata = new GeneratedFileMetadata("MyNamespace.MyCommand", DateTime.UtcNow);
        File.WriteAllText(_justGeneratedFilePath, $"{metadata.ToCommentLine()}\nexport class JustGenerated {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [_justGeneratedFilePath] = metadata
        };
    }

    void Because() => _orphanedFiles = FileMetadataScanner.FindOrphanedFiles(_tempDir, _generatedFiles);

    [Fact] void should_not_find_the_just_generated_file_as_orphaned() => _orphanedFiles.ShouldNotContain(_justGeneratedFilePath);
    [Fact] void should_not_find_any_orphaned_files() => _orphanedFiles.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class with_orphaned_generated_file : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;
    string _orphanedFilePath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _orphanedFilePath = Path.Combine(_tempDir, "OrphanedFile.ts");
        var metadata = new GeneratedFileMetadata("OldNamespace.OldClass", DateTime.UtcNow);
        File.WriteAllText(_orphanedFilePath, $"{metadata.ToCommentLine()}\nexport class OrphanedFile {{}}");

        var currentFilePath = Path.Combine(_tempDir, "CurrentFile.ts");
        var currentMetadata = new GeneratedFileMetadata("MyNamespace.MyClass", DateTime.UtcNow);
        File.WriteAllText(currentFilePath, $"{currentMetadata.ToCommentLine()}\nexport class CurrentFile {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [currentFilePath] = currentMetadata
        };
    }

    void Because() => _orphanedFiles = FileMetadataScanner.FindOrphanedFiles(_tempDir, _generatedFiles);

    [Fact] void should_find_orphaned_file() => _orphanedFiles.ShouldContain(_orphanedFilePath);
    [Fact] void should_find_exactly_one_orphaned_file() => _orphanedFiles.Count().ShouldEqual(1);

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

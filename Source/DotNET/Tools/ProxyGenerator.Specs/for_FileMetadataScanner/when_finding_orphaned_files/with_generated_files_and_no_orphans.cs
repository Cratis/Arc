// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class with_generated_files_and_no_orphans : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var filePath = Path.Combine(_tempDir, "TestFile.ts");
        var metadata = new GeneratedFileMetadata("MyNamespace.MyClass", DateTime.UtcNow);
        File.WriteAllText(filePath, $"{metadata.ToCommentLine()}\nexport class TestFile {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [filePath] = metadata
        };
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

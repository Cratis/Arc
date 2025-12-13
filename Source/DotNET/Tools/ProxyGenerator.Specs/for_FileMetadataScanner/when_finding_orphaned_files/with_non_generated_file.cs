// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class with_non_generated_file : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var manualFilePath = Path.Combine(_tempDir, "ManualFile.ts");
        File.WriteAllText(manualFilePath, "export class ManualFile {}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>();
    }

    void Because() => _orphanedFiles = FileMetadataScanner.FindOrphanedFiles(_tempDir, _generatedFiles);

    [Fact] void should_not_find_manual_file_as_orphaned() => _orphanedFiles.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

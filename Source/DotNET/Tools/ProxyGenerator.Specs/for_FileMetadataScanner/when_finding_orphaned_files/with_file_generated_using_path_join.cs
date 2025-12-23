// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_orphaned_files;

public class with_file_generated_using_path_join : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _orphanedFiles;
    string _generatedFilePath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        const string subDir = "commands";
        _generatedFilePath = Path.Join(_tempDir, subDir, "CreateOrder.ts");
        var directory = Path.GetDirectoryName(_generatedFilePath)!;
        Directory.CreateDirectory(directory);

        var metadata = new GeneratedFileMetadata("MyApp.Commands.CreateOrder", DateTime.UtcNow);
        File.WriteAllText(_generatedFilePath, $"{metadata.ToCommentLine()}\nexport class CreateOrder {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [_generatedFilePath] = metadata
        };
    }

    void Because() => _orphanedFiles = FileMetadataScanner.FindOrphanedFiles(_tempDir, _generatedFiles);

    [Fact] void should_not_find_the_just_generated_file_as_orphaned() => _orphanedFiles.ShouldNotContain(_generatedFilePath);
    [Fact] void should_not_find_any_orphaned_files() => _orphanedFiles.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

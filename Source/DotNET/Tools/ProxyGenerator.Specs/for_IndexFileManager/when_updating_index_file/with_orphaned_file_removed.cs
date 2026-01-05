// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_orphaned_file_removed : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Create initial file
        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");

        // Create index with both FileA and FileB (FileB will be deleted to simulate orphaned file)
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\nexport * from './FileB';\n");

        _messages = [];
    }

    void Because()
    {
        // FileB has been deleted from disk (simulating orphaned file removal)
        // Only FileA remains in the dictionary
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true),
        };

        IndexFileManager.UpdateIndexFile(_tempDir, dict, _messages.Add, _tempDir);
    }

    [Fact] void should_keep_file_a_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");
    [Fact] void should_remove_file_b_export() => File.ReadAllText(_indexPath).ShouldNotContain("export * from './FileB';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

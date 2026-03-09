// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_stale_export_to_nonexistent_file : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Create only FileA on disk
        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");

        // Create index with FileA and FileB, but FileB.ts does not exist on disk
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\nexport * from './FileB';\n");

        _messages = [];
    }

    void Because()
    {
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
        };

        IndexFileManager.UpdateIndexFile(_tempDir, dict, [], _messages.Add, _tempDir);
    }

    [Fact] void should_keep_existing_export_for_file_on_disk() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");
    [Fact] void should_remove_export_for_nonexistent_file() => File.ReadAllText(_indexPath).ShouldNotContain("export * from './FileB';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

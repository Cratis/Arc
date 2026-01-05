// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_orphaned_files_but_nothing_written : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Create index with multiple files, some will be orphaned
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\nexport * from './FileB';\nexport * from './FileC';\n");

        _messages = [];
    }

    void Because()
    {
        // FileA and FileC exist but weren't written (WasWritten=false)
        // FileB is orphaned
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
            [Path.Combine(_tempDir, "FileC.ts")] = new GeneratedFileMetadata("SourceC", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
        };

        var orphanedFiles = new List<string> { Path.Combine(_tempDir, "FileB.ts") };

        IndexFileManager.UpdateIndexFile(_tempDir, dict, orphanedFiles, _messages.Add, _tempDir);
    }

    [Fact] void should_keep_file_a_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");
    [Fact] void should_remove_orphaned_file_b_export() => File.ReadAllText(_indexPath).ShouldNotContain("export * from './FileB';");
    [Fact] void should_keep_file_c_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileC';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

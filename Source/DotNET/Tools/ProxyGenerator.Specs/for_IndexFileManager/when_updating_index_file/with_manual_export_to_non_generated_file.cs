// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_manual_export_to_non_generated_file : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");
        File.WriteAllText(Path.Combine(_tempDir, "ManualFile.ts"), "export class ManualFile {}");

        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\nexport * from './ManualFile';\n");

        _messages = [];
    }

    void Because()
    {
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true),
            [Path.Combine(_tempDir, "FileB.ts")] = new GeneratedFileMetadata("SourceB", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true),
        };

        IndexFileManager.UpdateIndexFile(_tempDir, dict, [], _messages.Add, _tempDir);
    }

    [Fact] void should_preserve_manual_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './ManualFile';");
    [Fact] void should_keep_generated_file_a_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");
    [Fact] void should_add_new_generated_file_b_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileB';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

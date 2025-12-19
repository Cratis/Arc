// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_existing_index_and_new_file : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");

        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\n");

        File.WriteAllText(Path.Combine(_tempDir, "FileB.ts"), "export class FileB {}");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        [Path.Combine(_tempDir, "FileA.ts"), Path.Combine(_tempDir, "FileB.ts")],
        _messages.Add,
        _tempDir);

    [Fact] void should_keep_existing_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");
    [Fact] void should_add_new_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileB';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

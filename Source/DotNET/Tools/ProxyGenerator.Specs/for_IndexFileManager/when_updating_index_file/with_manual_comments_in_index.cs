// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_manual_comments_in_index : Specification
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
        File.WriteAllText(_indexPath, "// Manual comment\nexport * from './FileA';\n");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        Directory.GetFiles(_tempDir, "*.ts").Where(f => Path.GetFileName(f) != "index.ts"),
        _messages.Add,
        _tempDir);

    [Fact] void should_preserve_manual_comment() => File.ReadAllText(_indexPath).ShouldContain("// Manual comment");
    [Fact] void should_keep_export() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

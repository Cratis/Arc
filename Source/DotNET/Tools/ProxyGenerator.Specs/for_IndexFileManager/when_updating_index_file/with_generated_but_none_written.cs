// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_generated_but_none_written : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;
    string _originalContent;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");

        _indexPath = Path.Combine(_tempDir, "index.ts");
        _originalContent = "// original content\nexport * from './FileA';\n";
        File.WriteAllText(_indexPath, _originalContent);

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

    [Fact] void should_leave_index_unchanged() => File.ReadAllText(_indexPath).ShouldEqual(_originalContent);

    void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_generated_but_none_written_and_no_index : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;
    bool _result;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");

        _indexPath = Path.Combine(_tempDir, "index.ts");
        _messages = [];
    }

    void Because()
    {
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
        };

        _result = IndexFileManager.UpdateIndexFile(_tempDir, dict, [], _messages.Add, _tempDir);
    }

    [Fact] void should_write_the_index() => _result.ShouldBeTrue();
    [Fact] void should_export_the_generated_file() => File.ReadAllText(_indexPath).ShouldContain("export * from './FileA';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }
}

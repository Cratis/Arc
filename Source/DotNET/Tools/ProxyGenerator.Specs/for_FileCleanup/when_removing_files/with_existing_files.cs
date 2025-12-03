// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileCleanup.when_removing_files;

public class with_existing_files : Specification
{
    string _outputPath;
    string _file1Path;
    string _file2Path;
    List<string> _messages;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_outputPath, "folder"));
        _file1Path = Path.Combine(_outputPath, "folder", "file1.ts");
        _file2Path = Path.Combine(_outputPath, "folder", "file2.ts");
        File.WriteAllText(_file1Path, "content1");
        File.WriteAllText(_file2Path, "content2");
        _messages = [];
    }

    void Because() => FileCleanup.RemoveFiles(_outputPath, ["folder/file1.ts"], _messages.Add);

    void Cleanup()
    {
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }
    }

    [Fact] void should_delete_removed_file() => File.Exists(_file1Path).ShouldBeFalse();
    [Fact] void should_keep_other_file() => File.Exists(_file2Path).ShouldBeTrue();
}

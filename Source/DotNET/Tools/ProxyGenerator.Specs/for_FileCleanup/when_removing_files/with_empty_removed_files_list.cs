// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileCleanup.when_removing_files;

public class with_empty_removed_files_list : Specification
{
    string _outputPath;
    string _filePath;
    List<string> _messages;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_outputPath, "folder"));
        _filePath = Path.Combine(_outputPath, "folder", "file.ts");
        File.WriteAllText(_filePath, "content");
        _messages = [];
    }

    void Because() => FileCleanup.RemoveFiles(_outputPath, [], _messages.Add);

    void Cleanup()
    {
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }
    }

    [Fact] void should_keep_existing_file() => File.Exists(_filePath).ShouldBeTrue();
    [Fact] void should_not_log_any_messages() => _messages.ShouldBeEmpty();
}

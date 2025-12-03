// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileCleanup.when_removing_files;

public class with_empty_folder_after_removal : Specification
{
    string _outputPath;
    string _folderPath;
    List<string> _messages;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _folderPath = Path.Combine(_outputPath, "folder");
        Directory.CreateDirectory(_folderPath);
        File.WriteAllText(Path.Combine(_folderPath, "file.ts"), "content");
        _messages = [];
    }

    void Because() => FileCleanup.RemoveFiles(_outputPath, ["folder/file.ts"], _messages.Add);

    void Cleanup()
    {
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }
    }

    [Fact] void should_remove_empty_folder() => Directory.Exists(_folderPath).ShouldBeFalse();
}

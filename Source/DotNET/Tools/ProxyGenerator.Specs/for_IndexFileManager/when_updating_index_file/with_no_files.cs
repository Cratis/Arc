// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_no_files : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './FileA';\n");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        new Dictionary<string, GeneratedFileMetadata>(),
        _messages.Add,
        _tempDir);

    [Fact] void should_delete_index_file() => File.Exists(_indexPath).ShouldBeFalse();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

/// <summary>
/// When a directory has no generated files and the existing index.ts was not created by
/// the proxy generator (no @generated marker), the generator must not touch it.
/// </summary>
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
        [],
        _messages.Add,
        _tempDir);

    [Fact] void should_preserve_hand_written_index_file() => File.Exists(_indexPath).ShouldBeTrue();
    [Fact] void should_not_log_deletion() => _messages.ShouldNotContain(m => m.Contains("Deleted"));

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

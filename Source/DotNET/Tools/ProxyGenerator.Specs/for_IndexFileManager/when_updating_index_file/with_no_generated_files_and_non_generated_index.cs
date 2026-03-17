// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

/// <summary>
/// Verifies that a hand-written (non-generated) index.ts is never deleted even when
/// it only has wildcard exports that all point to files that still exist on disk.
/// The generator must not touch any index.ts it did not create.
/// </summary>
public class with_no_generated_files_and_non_generated_index : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Hand-written index.ts with no generated marker
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './MyComponent';\n");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        new Dictionary<string, GeneratedFileMetadata>(),
        [],
        _messages.Add,
        _tempDir);

    [Fact] void should_preserve_index_file() => File.Exists(_indexPath).ShouldBeTrue();
    [Fact] void should_not_log_deletion() => _messages.ShouldNotContain(m => m.Contains("Deleted"));

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

/// <summary>
/// When a directory has no generated files and the existing index.ts WAS created
/// by the proxy generator (has the @generated marker), it should be deleted to clean up.
/// </summary>
public class with_no_files_but_generated_index : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _indexPath = Path.Combine(_tempDir, "index.ts");

        // Write an index.ts that has the generated marker
        var metadata = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash("export * from './FileA';"), true);
        File.WriteAllText(_indexPath, $"{metadata.ToCommentLine()}\nexport * from './FileA';\n");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        new Dictionary<string, GeneratedFileMetadata>(),
        [],
        _messages.Add,
        _tempDir);

    [Fact] void should_delete_generated_index_file() => File.Exists(_indexPath).ShouldBeFalse();
    [Fact] void should_log_deletion() => _messages.ShouldContain(m => m.Contains("Deleted"));

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

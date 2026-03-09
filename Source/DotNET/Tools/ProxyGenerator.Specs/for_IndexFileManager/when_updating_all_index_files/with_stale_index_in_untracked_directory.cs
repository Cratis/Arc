// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_all_index_files;

public class with_stale_index_in_untracked_directory : Specification
{
    string _outputPath;
    string _trackedDir;
    string _untrackedDir;
    List<string> _messages;
    string _untrackedIndexPath;
    string _trackedIndexPath;

    void Establish()
    {
        _outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _trackedDir = Path.Combine(_outputPath, "tracked");
        _untrackedDir = Path.Combine(_outputPath, "untracked");
        Directory.CreateDirectory(_trackedDir);
        Directory.CreateDirectory(_untrackedDir);

        // Tracked directory has a generated file
        File.WriteAllText(Path.Combine(_trackedDir, "FileA.ts"), "export class FileA {}");
        _trackedIndexPath = Path.Combine(_trackedDir, "index.ts");

        // Untracked directory has a stale index pointing to a file that no longer exists
        _untrackedIndexPath = Path.Combine(_untrackedDir, "index.ts");
        File.WriteAllText(_untrackedIndexPath, "export * from './OldFile';\n");

        _messages = [];
    }

    void Because()
    {
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_trackedDir, "FileA.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true),
        };

        // Only pass tracked directory; untracked should be discovered automatically
        IndexFileManager.UpdateAllIndexFiles([_trackedDir], dict, [], _messages.Add, _outputPath);
    }

    [Fact] void should_create_tracked_index() => File.Exists(_trackedIndexPath).ShouldBeTrue();
    [Fact] void should_remove_stale_untracked_index() => File.Exists(_untrackedIndexPath).ShouldBeFalse();

    void Cleanup()
    {
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, true);
        }
    }
}

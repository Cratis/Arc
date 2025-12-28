// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_new_files : Specification
{
    string _tempDir;
    List<string> _messages;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "FileA.ts"), "export class FileA {}");
        File.WriteAllText(Path.Combine(_tempDir, "FileB.ts"), "export class FileB {}");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        [Path.Combine(_tempDir, "FileA.ts"), Path.Combine(_tempDir, "FileB.ts")],
        _messages.Add,
        _tempDir);

    [Fact] void should_create_index_file() => File.Exists(Path.Combine(_tempDir, "index.ts")).ShouldBeTrue();
    [Fact] void should_contain_export_for_file_a() => File.ReadAllText(Path.Combine(_tempDir, "index.ts")).ShouldContain("export * from './FileA';");
    [Fact] void should_contain_export_for_file_b() => File.ReadAllText(Path.Combine(_tempDir, "index.ts")).ShouldContain("export * from './FileB';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

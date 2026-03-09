// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_no_generated_files_but_live_tsx_exports : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Hand-written .tsx component that exists on disk
        File.WriteAllText(Path.Combine(_tempDir, "MyComponent.tsx"), "export const MyComponent = () => <div />;");

        // Hand-written index.ts that re-exports the component
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
    [Fact] void should_preserve_index_content() => File.ReadAllText(_indexPath).ShouldContain("export * from './MyComponent';");

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

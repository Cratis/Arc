// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

/// <summary>
/// Verifies that a hand-written index.ts using named exports (e.g. export { Foo } from './Foo')
/// is never deleted by the proxy generator, even when the directory contains no generated files.
/// This matches the real-world pattern used in Components folders (export { X } from './X').
/// </summary>
public class with_no_generated_files_but_named_exports_in_index : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Hand-written .tsx component that exists on disk
        File.WriteAllText(Path.Combine(_tempDir, "Chat.tsx"), "export const Chat = () => null;");

        // Hand-written index.ts using named exports (not export * — this is what
        // the old ExportRegex failed to recognise, causing the file to be deleted)
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(
            _indexPath,
            "// Copyright (c) Cratis. All rights reserved.\nexport { Chat } from './Chat';\nexport type { ChatProps } from './Chat';\n");

        _messages = [];
    }

    void Because() => IndexFileManager.UpdateIndexFile(
        _tempDir,
        new Dictionary<string, GeneratedFileMetadata>(),
        [],
        _messages.Add,
        _tempDir);

    [Fact] void should_preserve_index_file() => File.Exists(_indexPath).ShouldBeTrue();
    [Fact] void should_preserve_named_export() => File.ReadAllText(_indexPath).ShouldContain("export { Chat } from './Chat';");
    [Fact] void should_not_log_deletion() => _messages.ShouldNotContain(m => m.Contains("Deleted"));

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

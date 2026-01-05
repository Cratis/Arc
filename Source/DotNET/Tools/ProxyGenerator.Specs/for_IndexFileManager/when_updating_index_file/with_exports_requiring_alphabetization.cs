// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_IndexFileManager.when_updating_index_file;

public class with_exports_requiring_alphabetization : Specification
{
    string _tempDir;
    List<string> _messages;
    string _indexPath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        // Create index with exports in non-alphabetical order
        _indexPath = Path.Combine(_tempDir, "index.ts");
        File.WriteAllText(_indexPath, "export * from './Zebra';\nexport * from './Apple';\nexport * from './Mango';\n");

        _messages = [];
    }

    void Because()
    {
        // All files exist, add new ones (Banana, Cherry, Ant) - they should be alphabetized among themselves
        var dict = new Dictionary<string, GeneratedFileMetadata>
        {
            [Path.Combine(_tempDir, "Zebra.ts")] = new GeneratedFileMetadata("SourceZ", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
            [Path.Combine(_tempDir, "Apple.ts")] = new GeneratedFileMetadata("SourceA", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
            [Path.Combine(_tempDir, "Mango.ts")] = new GeneratedFileMetadata("SourceM", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), false),
            [Path.Combine(_tempDir, "Banana.ts")] = new GeneratedFileMetadata("SourceB", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true), // Written
            [Path.Combine(_tempDir, "Cherry.ts")] = new GeneratedFileMetadata("SourceC", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true), // Written
            [Path.Combine(_tempDir, "Ant.ts")] = new GeneratedFileMetadata("SourceAnt", DateTime.UtcNow, GeneratedFileMetadata.ComputeHash(""), true), // Written
        };

        IndexFileManager.UpdateIndexFile(_tempDir, dict, [], _messages.Add, _tempDir);
    }

    [Fact] void should_preserve_existing_export_order()
    {
        var content = File.ReadAllText(_indexPath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var exports = lines.Where(l => l.StartsWith("export")).ToList();

        // Existing exports should retain their original order
        exports[0].ShouldEqual("export * from './Zebra';");
        exports[1].ShouldEqual("export * from './Apple';");
        exports[2].ShouldEqual("export * from './Mango';");
    }

    [Fact] void should_alphabetize_new_exports_among_themselves()
    {
        var content = File.ReadAllText(_indexPath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var exports = lines.Where(l => l.StartsWith("export")).ToList();

        // New exports should be alphabetized among themselves and appear after existing ones
        exports[3].ShouldEqual("export * from './Ant';");
        exports[4].ShouldEqual("export * from './Banana';");
        exports[5].ShouldEqual("export * from './Cherry';");
    }

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

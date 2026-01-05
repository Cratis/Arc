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
        // All files exist, add new ones (Banana should go between Apple and Mango, Cherry after Mango, Ant before Apple/after Zebra)
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

    [Fact] void should_insert_new_exports_at_correct_alphabetical_positions()
    {
        var content = File.ReadAllText(_indexPath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var exports = lines.Where(l => l.StartsWith("export")).ToList();

        // New exports are inserted before the first existing export that is alphabetically greater
        // Ant, Banana, Cherry are all < Zebra (first existing), so they all go before it
        exports[0].ShouldEqual("export * from './Ant';");
        exports[1].ShouldEqual("export * from './Banana';");
        exports[2].ShouldEqual("export * from './Cherry';");
        exports[3].ShouldEqual("export * from './Zebra';");   // First existing
        exports[4].ShouldEqual("export * from './Apple';");   // Second existing
        exports[5].ShouldEqual("export * from './Mango';");   // Third existing
    }

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_superseded_files;

public class with_descriptor_name_matching_source_file : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _supersededFiles;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(_tempDir, "System");
        Directory.CreateDirectory(subDir);

        // The source file name equals the descriptor name — no redirect happened
        var currentFilePath = Path.GetFullPath(Path.Combine(subDir, "AllProspects.ts"));
        var metadata = new GeneratedFileMetadata("Core.AllProspects", DateTime.UtcNow);
        File.WriteAllText(currentFilePath, $"{metadata.ToCommentLine()}\nexport class AllProspects {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [currentFilePath] = metadata
        };
    }

    void Because() => _supersededFiles = FileMetadataScanner.FindSupersededFiles(
        _tempDir,
        segmentsToSkip: 0,
        [new TestDescriptor(typeof(string), "AllProspects")],
        new Dictionary<string, string> { ["System.String"] = "AllProspects" },
        _generatedFiles);

    [Fact] void should_find_no_superseded_files() => _supersededFiles.ShouldBeEmpty();

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

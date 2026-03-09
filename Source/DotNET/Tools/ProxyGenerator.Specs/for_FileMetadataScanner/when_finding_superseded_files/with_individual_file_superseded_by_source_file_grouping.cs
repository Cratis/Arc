// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner.when_finding_superseded_files;

public class with_individual_file_superseded_by_source_file_grouping : Specification
{
    string _tempDir;
    Dictionary<string, GeneratedFileMetadata> _generatedFiles;
    IEnumerable<string> _supersededFiles;
    string _staleFilePath;
    string _currentFilePath;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // ResolveTargetPath for typeof(string) with segmentsToSkip=1 produces the root separator
        var subDir = Path.Combine(_tempDir, "System");
        Directory.CreateDirectory(subDir);

        // The old per-type file that should be superseded (no @generated marker, just like the user's AllProspects.ts)
        _staleFilePath = Path.Combine(subDir, "AllProspects.ts");
        File.WriteAllText(_staleFilePath, "export class AllProspects {}");

        // The new source-file-based output that contains AllProspects
        _currentFilePath = Path.GetFullPath(Path.Combine(subDir, "Listing.ts"));
        var metadata = new GeneratedFileMetadata("Core.Listing.AllProspects", DateTime.UtcNow);
        File.WriteAllText(_currentFilePath, $"{metadata.ToCommentLine()}\nexport class AllProspects {{}}");

        _generatedFiles = new Dictionary<string, GeneratedFileMetadata>
        {
            [_currentFilePath] = metadata
        };
    }

    void Because() => _supersededFiles = FileMetadataScanner.FindSupersededFiles(
        _tempDir,
        segmentsToSkip: 0,
        [new TestDescriptor(typeof(string), "AllProspects")],
        new Dictionary<string, string> { ["System.String"] = "Listing" },
        _generatedFiles);

    [Fact] void should_find_the_stale_file() => _supersededFiles.ShouldContain(Path.GetFullPath(_staleFilePath));
    [Fact] void should_find_exactly_one_superseded_file() => _supersededFiles.Count().ShouldEqual(1);

    void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}

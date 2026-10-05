// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_FileMetadataScanner;

public class when_pruning_orphans_with_authored_marker_literals : Specification
{
    const string Copyright = "// Copyright (c) Cratis. All rights reserved.";
    const string License = "// Licensed under the MIT license. See LICENSE file in the project root for full license information.";
    string _directory = null!;
    Dictionary<string, string> _authored = [];
    string[] _generated = [];
    string[] _orphans = [];
    int _removed;
    bool _rejected;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        var marker = new GeneratedFileMetadata("Sample", new DateTime(2020, 1, 1), "ABC").ToCommentLine();
        foreach (var newline in new[] { "\n", "\r\n" })
        {
            string[][] unsafeHeaders =
            [
                [Copyright, License, "export const example = `", marker, "`;"],
                ["// Copyright (c) Cratis. Not the standard copyright.", License, "", marker],
                [Copyright, "// Not the standard license", "", marker]
            ];
            foreach (var lines in unsafeHeaders)
            {
                var path = Path.Combine(_directory, $"Authored{_authored.Count}.ts");
                var content = string.Join(newline, lines);
                _authored.Add(path, content);
                File.WriteAllText(path, content);
            }
            var generated = Path.Combine(_directory, $"Generated{_generated.Length}.ts");
            File.WriteAllText(generated, string.Join(newline, Copyright, License, "", marker));
            _generated = [.. _generated, generated];
        }
    }

    void Because()
    {
        _rejected = _authored.Keys.All(path => !GeneratedFileMetadata.IsGeneratedFile(path, out _));
        _orphans = [.. FileMetadataScanner.FindOrphanedFiles(_directory, new Dictionary<string, GeneratedFileMetadata>())];
        _removed = FileMetadataScanner.RemoveOrphanedFiles(_directory, _orphans, _ => { });
    }

    [Fact] void should_reject_all_six_unsafe_headers_in_metadata_recognition() => _rejected.ShouldBeTrue();
    [Fact] void should_find_only_the_two_real_generated_orphans() => _orphans.ShouldContainOnly(_generated);
    [Fact] void should_actually_remove_the_two_generated_orphans() => _removed.ShouldEqual(2);
    [Fact] void should_delete_real_generated_files() => _generated.All(path => !File.Exists(path)).ShouldBeTrue();
    [Fact] void should_keep_all_six_authored_files_byte_for_byte() => _authored.All(pair => File.Exists(pair.Key) && File.ReadAllText(pair.Key) == pair.Value).ShouldBeTrue();

    void Destroy() => Directory.Delete(_directory, true);
}

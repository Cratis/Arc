// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileMetadata;

public class when_recognizing_generated_files : Specification
{
    string _directory = null!;
    GeneratedFileMetadata _metadata = new("Sample", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), "ABC");
    bool _legacy;
    bool _current;
    bool _literal;
    bool _lateMarker;
    GeneratedFileMetadata? _legacyMetadata;
    GeneratedFileMetadata? _currentMetadata;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "legacy.ts"), $"{_metadata.ToCommentLine()}\nexport class Sample {{}}");
        File.WriteAllText(Path.Combine(_directory, "current.ts"), $"// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n{_metadata.ToCommentLine()}\nexport class Sample {{}}");
        File.WriteAllText(Path.Combine(_directory, "literal.ts"), $"export const marker = '{_metadata.ToCommentLine()}';");
        File.WriteAllText(Path.Combine(_directory, "late.ts"), $"// Copyright (c) Cratis. All rights reserved.\n// License\n\nexport const value = 1;\n{_metadata.ToCommentLine()}");
    }

    void Because()
    {
        _legacy = GeneratedFileMetadata.IsGeneratedFile(Path.Combine(_directory, "legacy.ts"), out _legacyMetadata);
        _current = GeneratedFileMetadata.IsGeneratedFile(Path.Combine(_directory, "current.ts"), out _currentMetadata);
        _literal = GeneratedFileMetadata.IsGeneratedFile(Path.Combine(_directory, "literal.ts"), out _);
        _lateMarker = GeneratedFileMetadata.IsGeneratedFile(Path.Combine(_directory, "late.ts"), out _);
    }

    [Fact] void should_recognize_the_legacy_first_line_marker() => _legacy.ShouldBeTrue();
    [Fact] void should_recognize_the_license_first_marker() => _current.ShouldBeTrue();
    [Fact] void should_preserve_legacy_metadata() => (_legacyMetadata! with { GeneratedTime = _legacyMetadata!.GeneratedTime.ToUniversalTime() }).ShouldEqual(_metadata);
    [Fact] void should_preserve_current_metadata() => (_currentMetadata! with { GeneratedTime = _currentMetadata!.GeneratedTime.ToUniversalTime() }).ShouldEqual(_metadata);
    [Fact] void should_not_recognize_a_marker_in_a_literal() => _literal.ShouldBeFalse();
    [Fact] void should_not_recognize_a_marker_after_the_header() => _lateMarker.ShouldBeFalse();

    void Destroy() => Directory.Delete(_directory, true);
}

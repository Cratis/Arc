// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_GeneratedFileMetadata;

public class when_recognizing_a_configured_header : Specification
{
    string _directory = null!;
    string _path = null!;
    GeneratedFileMetadata _expected = new("Sample", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), "ABC");
    GeneratedFileMetadata? _actual;
    bool _recognized;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "Sample.ts");
        var header = string.Join("\r\n", Enumerable.Range(1, 12).Select(index => $"// Application line {index}"));
        File.WriteAllText(_path, $"{header}\r\n\r\n{FileHeader.GeneratedNotice}\r\n{_expected.ToCommentLine()}\r\nexport class Sample {{}}");
    }

    void Because() => _recognized = GeneratedFileMetadata.IsGeneratedFile(_path, out _actual);

    [Fact] void should_recognize_headers_longer_than_the_old_four_line_envelope() => _recognized.ShouldBeTrue();
    [Fact] void should_preserve_the_metadata() => (_actual! with { GeneratedTime = _actual!.GeneratedTime.ToUniversalTime() }).ShouldEqual(_expected);

    void Destroy() => Directory.Delete(_directory, true);
}

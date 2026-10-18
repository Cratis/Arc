// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_GenerationFingerprint;

public class when_a_file_it_covers_changes : Specification
{
    string _directory;
    string _file;
    string _before;
    string _after;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory().FullName;
        _file = Path.Join(_directory, "Source.cs");
        File.WriteAllText(_file, "class A;");
        File.SetLastWriteTimeUtc(_file, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _before = new GenerationFingerprint().WithFiles("Sources", [_file]).ToString();
    }

    void Because()
    {
        File.WriteAllText(_file, "class AB;");
        File.SetLastWriteTimeUtc(_file, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        _after = new GenerationFingerprint().WithFiles("Sources", [_file]).ToString();
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_change_the_fingerprint() => _after.ShouldNotEqual(_before);
}

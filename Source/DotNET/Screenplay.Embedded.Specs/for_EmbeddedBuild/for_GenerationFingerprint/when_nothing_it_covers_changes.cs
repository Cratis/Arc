// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_GenerationFingerprint;

public class when_nothing_it_covers_changes : Specification
{
    string _directory;
    string _first;
    string _second;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory().FullName;
        File.WriteAllText(Path.Join(_directory, "A.cs"), "class A;");
        File.WriteAllText(Path.Join(_directory, "B.cs"), "class B;");
    }

    void Because()
    {
        var a = Path.Join(_directory, "A.cs");
        var b = Path.Join(_directory, "B.cs");
        _first = new GenerationFingerprint().WithSetting("AssemblyName", "App").WithFiles("Sources", [a, b]).ToString();
        _second = new GenerationFingerprint().WithSetting("AssemblyName", "App").WithFiles("Sources", [b, a]).ToString();
    }

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_be_the_same_whatever_order_the_files_are_given_in() => _second.ShouldEqual(_first);
}

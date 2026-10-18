// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_ReusableGeneration;

public class when_reusing_with_the_same_fingerprint : Specification
{
    string _directory;
    bool _reusable;
    IReadOnlyList<EmbeddedResourceFile> _reused;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory().FullName;
        var document = Path.Combine(_directory, "document.play");
        File.WriteAllText(document, "application");
        ReusableGeneration.Remember(_directory, "ABC", [new(document, "Logical.Name")]);
    }

    void Because() => _reusable = ReusableGeneration.TryReuse(_directory, "ABC", out _reused);

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_be_reusable() => _reusable.ShouldBeTrue();
    [Fact] void should_return_the_file() => _reused.Single().Path.ShouldEqual(Path.Combine(_directory, "document.play"));
    [Fact] void should_return_its_logical_name() => _reused.Single().LogicalName.ShouldEqual("Logical.Name");
}

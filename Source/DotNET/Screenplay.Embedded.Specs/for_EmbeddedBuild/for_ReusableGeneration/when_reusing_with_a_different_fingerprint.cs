// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_ReusableGeneration;

public class when_reusing_with_a_different_fingerprint : Specification
{
    string _directory;
    bool _reusable;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory().FullName;
        var document = Path.Join(_directory, "document.play");
        File.WriteAllText(document, "application");
        ReusableGeneration.Remember(_directory, "ABC", [new(document, "Logical.Name")]);
    }

    void Because() => _reusable = ReusableGeneration.TryReuse(_directory, "DEF", out _);

    void Destroy() => Directory.Delete(_directory, true);

    [Fact] void should_not_be_reusable() => _reusable.ShouldBeFalse();
}

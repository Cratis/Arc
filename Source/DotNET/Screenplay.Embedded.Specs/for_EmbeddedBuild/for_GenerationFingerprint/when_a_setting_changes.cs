// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_GenerationFingerprint;

public class when_a_setting_changes : Specification
{
    string _before;
    string _after;

    void Because()
    {
        _before = new GenerationFingerprint().WithSetting("AuthoringOnlyConstructs", "False").ToString();
        _after = new GenerationFingerprint().WithSetting("AuthoringOnlyConstructs", "True").ToString();
    }

    [Fact] void should_change_the_fingerprint() => _after.ShouldNotEqual(_before);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayNaming.when_making_a_file_path;

/// <summary>
/// A file reference is written verbatim, so a Windows separator would travel into a document read on any platform.
/// </summary>
public class from_a_declared_path : given.a_naming
{
    string? _withBackslashes;
    string? _withNothing;

    void Because()
    {
        _withBackslashes = _naming.ToFilePath(@"Authors\Registration\Registration.cs");
        _withNothing = _naming.ToFilePath("   ");
    }

    [Fact] void should_normalize_the_separator() => _withBackslashes.ShouldEqual("Authors/Registration/Registration.cs");
    [Fact] void should_treat_whitespace_only_as_absent() => _withNothing.ShouldBeNull();
}

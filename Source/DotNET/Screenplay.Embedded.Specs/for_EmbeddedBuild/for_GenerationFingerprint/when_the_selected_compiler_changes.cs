// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.Build;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild.for_GenerationFingerprint;

public class when_the_selected_compiler_changes : Specification
{
    string _before;
    string _after;

    void Because()
    {
        _before = new GenerationFingerprint().WithCompiler(new AssemblyName("Microsoft.CodeAnalysis, Version=5.9.0.0")).ToString();
        _after = new GenerationFingerprint().WithCompiler(new AssemblyName("Microsoft.CodeAnalysis, Version=5.12.0.0")).ToString();
    }

    [Fact] void should_invalidate_the_cached_generation() => _after.ShouldNotEqual(_before);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionMsBuild.when_emitting;

public class with_off : given.a_read_protection_build
{
    void Establish() => _buildMode = "Off";
    Task Because() => GenerateAndResolve();
    [Fact] void should_compile_the_enum_argument_for_reflection() => _runtime.ShouldEqual("0:0:");
    [Fact] void should_resolve_the_emitted_attribute_by_roslyn() => _roslyn.ShouldEqual("0:0:");
}

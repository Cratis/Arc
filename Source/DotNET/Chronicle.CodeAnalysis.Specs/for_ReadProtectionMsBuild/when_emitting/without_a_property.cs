// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionMsBuild.when_emitting;

public class without_a_property : given.a_read_protection_build
{
    Task Because() => GenerateAndResolve();
    [Fact] void should_preserve_the_legacy_profile_by_reflection() => _runtime.ShouldEqual("0:0:");
    [Fact] void should_preserve_the_legacy_profile_by_roslyn() => _roslyn.ShouldEqual("0:0:");
}

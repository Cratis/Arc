// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionMsBuild.when_emitting;

public class with_the_chronicle_package_and_no_property : given.a_read_protection_build
{
    void Establish() => _importChroniclePackageProps = true;
    Task Because() => GenerateAndResolve();
    [Fact] void should_default_read_protection_to_on_by_reflection() => _runtime.ShouldEqual("0:2:");
    [Fact] void should_default_read_protection_to_on_by_roslyn() => _roslyn.ShouldEqual("0:2:");
}

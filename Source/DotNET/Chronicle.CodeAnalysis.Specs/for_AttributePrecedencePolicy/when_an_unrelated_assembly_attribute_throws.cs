// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy;

public class when_an_unrelated_assembly_attribute_throws : given.a_declaration
{
    void Establish() => _source = "[assembly: Exploding] [assembly: ReadProtection(ReadProtectionMode.On)] [System.AttributeUsage(System.AttributeTargets.Assembly)] public class ExplodingAttribute : System.Attribute { public ExplodingAttribute() => throw new System.InvalidOperationException(); } public class Command; public class Model;";
    void Because() => Resolve();
    [Fact] void should_only_instantiate_the_read_protection_assembly_attribute() => _runtime.ShouldEqual("0:2:");
    [Fact] void should_agree_with_roslyn() => _roslyn.ShouldEqual(_runtime);
}

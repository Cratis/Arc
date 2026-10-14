// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_protection;

public class from_a_generic_base_definition : given.a_declaration
{
    void Establish() => _source = "[ReadProtection(ReadProtectionMode.On)] public class Base<T>; public class Command : Base<Model>; public class Model;";
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("0:2:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("0:2:");
}

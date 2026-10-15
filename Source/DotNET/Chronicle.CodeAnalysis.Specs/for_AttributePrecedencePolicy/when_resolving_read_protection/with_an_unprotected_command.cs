// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_protection;

public class with_an_unprotected_command : given.a_declaration
{
    void Establish() => _source = "[assembly: ReadProtection(ReadProtectionMode.On)] [Unprotected] public class Command; public class Model;";
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("2:0:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("2:0:");
}

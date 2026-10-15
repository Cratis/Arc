// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_protection;

public class from_a_base_command : given.a_declaration
{
    void Establish() => _source = "[ReadProtection(ReadProtectionMode.Measure)] public class Base; public class Command : Base; public class Model;";
    void Because() => Resolve();
    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("0:1:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("0:1:");
}

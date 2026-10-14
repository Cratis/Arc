// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_protection;

public class with_a_protected_decision_under_on : given.a_declaration
{
    void Establish() => _source = "[ProtectedDecision, ReadProtection(ReadProtectionMode.On)] public class Command; public class Model;";
    void Because() => Resolve();
    [Fact] void should_not_conflict_by_reflection() => _runtime.ShouldEqual("1:2:");
    [Fact] void should_not_conflict_by_roslyn() => _roslyn.ShouldEqual("1:2:");
}

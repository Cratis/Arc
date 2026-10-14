// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_key_guard;

public class on_a_derived_command_of_a_protected_base : given.a_declaration
{
    void Establish()
    {
        _action = "key_guard";
        _source = """
            [ProtectedDecision] public class Base; [KeyGuard(KeyGuardMode.On)] public class Command : Base; public class Model;
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("1:0:CommandCannotCombineProtectedDecisionAndKeyGuard");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("1:0:CommandCannotCombineProtectedDecisionAndKeyGuard");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_key_guard;

public class from_a_base_command : given.a_declaration
{
    void Establish()
    {
        _action = "key_guard";
        _source = """
            [KeyGuard(KeyGuardMode.On)] public class Base; public class Command : Base; public class Model;
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("0:2:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("0:2:");
}

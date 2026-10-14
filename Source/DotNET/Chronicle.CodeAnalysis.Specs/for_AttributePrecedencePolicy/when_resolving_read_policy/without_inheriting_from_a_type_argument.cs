// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_policy;

public class without_inheriting_from_a_type_argument : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = """
            public class Model<T>; [Consistency(ReadConsistency.Stored)] public class Argument;
            [KeyGuard(KeyGuardMode.On)] public class Command { public string Key { get; } = "key"; public void Handle(Model<Argument> model) { } }
            """;
        _genericModel = true;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("1:True:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("1:True:");
}

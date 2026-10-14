// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_policy;

public class from_a_base_read_model : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = """
            [Consistency(ReadConsistency.Stored)] public class BaseModel; public class Model : BaseModel;
            [KeyGuard(KeyGuardMode.On)] public class Command { public string Key { get; } = "key"; public void Handle(Model model) { } }
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("0:True:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("0:True:");
}

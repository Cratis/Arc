// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_policy;

public class for_read_models_in_a_validator : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = """
            public class Model;
            [KeyGuard(KeyGuardMode.On)][Consistency(ReadConsistency.Current)] public class Command { public string Key { get; } = "key"; [Consistency(ReadConsistency.Stored)] public void Handle(Model model) { } }
            """;
        _hasMethod = false;
        _hasParameter = false;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("1:True:");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("1:True:");
}

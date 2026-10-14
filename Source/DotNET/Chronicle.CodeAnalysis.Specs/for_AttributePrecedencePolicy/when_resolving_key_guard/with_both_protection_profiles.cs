// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_key_guard;

public class with_both_protection_profiles : given.a_declaration
{
    void Establish()
    {
        _action = "key_guard";
        _source = """
            public class Model;
            [ProtectedDecision][Unprotected] public class Command { public string Key { get; } = "key"; public void Handle(Model model) { } }
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("1:0:CommandCannotBeBothProtectedAndUnprotected");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("1:0:CommandCannotBeBothProtectedAndUnprotected");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_resolving_read_policy;

public class with_both_on_one_parameter : given.a_declaration
{
    void Establish()
    {
        _action = "read_policy";
        _source = """
            public class Model;
            [KeyGuard(KeyGuardMode.On)] public class Command { public string Key { get; } = "key"; public void Handle([Unprotected][Consistency(ReadConsistency.Current)] Model model) { } }
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("0:False:ConflictingReadPolicy");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("0:False:ConflictingReadPolicy");
}

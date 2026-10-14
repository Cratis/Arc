// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // These declarations are source snippets, independent of platform line endings.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_AttributePrecedencePolicy.when_looking_up_by;

public class with_a_missing_property : given.a_declaration
{
    void Establish()
    {
        _action = "lookup";
        _source = """
            public class Model;
             public class Command { public string Key { get; } = "key"; public void Handle([LookUpBy("Missing")] Model model) { } }
            """;
    }

    void Because() => Resolve();

    [Fact] void should_resolve_by_reflection() => _runtime.ShouldEqual("InvalidLookUpBy");
    [Fact] void should_resolve_by_roslyn() => _roslyn.ShouldEqual("InvalidLookUpBy");
}

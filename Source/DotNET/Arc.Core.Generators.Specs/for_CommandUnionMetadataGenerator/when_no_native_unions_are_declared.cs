// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandUnionMetadataGenerator;

public class when_no_native_unions_are_declared : Specification
{
    string _source;
    Diagnostic[] _diagnostics;

    void Because()
    {
        var result = given.union_compilation.Generate(given.union_compilation.Create(
            """
            using Cratis.Arc.Commands.ModelBound;
            [Command] public class Ordinary { public int Handle() => 42; }
            """,
            "NoNativeUnions"));
        _source = result.Source;
        _diagnostics = result.Diagnostics;
    }

    [Fact] void should_emit_no_native_union_references_on_either_framework() => _source.ShouldBeEmpty();
    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
}

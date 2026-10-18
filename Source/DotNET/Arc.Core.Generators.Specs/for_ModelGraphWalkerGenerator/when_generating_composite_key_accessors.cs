// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_generating_composite_key_accessors : Specification
{
    const string Models = """
        namespace Keys
        {
            public class Base { public string Inherited { get; } = "base"; }
            public class Composite : Base
            {
                public string Part { get; } = "value";
                public string Hidden { private get; set; } = "*";
                public string this[int index] => "*";
                public static string Static => "*";
            }
            [Cratis.Arc.Commands.ModelBound.Command]
            public record Change([Cratis.Chronicle.Keys.Key] Composite Key) { public void Handle() { } }
        }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics) = CompositeKeyCompilation.Compile("CompositeCoverage", Models);

    [Fact] void should_emit_compilable_accessors() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_exact_type() => _source.ShouldContain(".Register<global::Keys.Composite>(");
    [Fact] void should_include_inherited_public_getters() => _source.ShouldContain("global::Keys.Base)instance).@Inherited");
    [Fact] void should_include_own_public_getters() => _source.ShouldContain("global::Keys.Composite)instance).@Part");
    [Fact] void should_exclude_non_public_getters() => _source.ShouldNotContain(".@Hidden");
    [Fact] void should_exclude_static_properties() => _source.ShouldNotContain(".@Static");
    [Fact] void should_exclude_indexers() => _source.ShouldNotContain(".@Item");
    [Fact] void should_record_declared_types_for_startup_without_command_getters() => _source.ShouldContain(".DeclareKeyTypes(typeof(global::Keys.Change), typeof(global::Keys.Composite))");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_generating_command_key_properties : Specification
{
    const string Models = """
        namespace Keys
        {
            public class Base { public System.Guid Inherited { get; } = System.Guid.Empty; }
            [Cratis.Arc.Commands.ModelBound.Command]
            public class Change : Base
            {
                public string Part { get; set; } = "value";
                public int? Count { get; set; }
                public string Hidden { private get; set; } = "*";
                public string this[int index] => "*";
                public static string Static => "*";
                public void Handle() { }
            }
        }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile("CommandKeyProperties", Models);

    [Fact] void should_emit_compilable_registrations() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_command() => _source.ShouldContain(".RegisterCommand(typeof(global::Keys.Change)");
    [Fact] void should_include_own_properties_with_their_types() => _source.ShouldContain("(\"Part\", typeof(string), static instance => ((global::Keys.Change)instance).@Part");
    [Fact] void should_include_nullable_value_types() => _source.ShouldContain("typeof(int?)");
    [Fact] void should_include_inherited_properties() => _source.ShouldContain("(global::Keys.Base)(global::Keys.Change)instance).@Inherited");
    [Fact] void should_exclude_non_public_getters() => _source.ShouldNotContain(".@Hidden");
    [Fact] void should_exclude_static_properties() => _source.ShouldNotContain(".@Static");
    [Fact] void should_exclude_indexers() => _source.ShouldNotContain(".@Item");
}

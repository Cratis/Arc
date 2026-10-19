// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

public class when_models_declare_keys : Specification
{
    const string Models = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Models;
        public class WithId { public System.Guid Id { get; set; } }
        public class WithoutId { public string Name { get; set; } = string.Empty; }
        public class WithHiddenId { internal int Id { get; set; } }
        [Command] public record Change(System.Guid Key) { public void Handle(WithId withId, WithoutId withoutId, WithHiddenId hidden) { } }
        """;

    string _source;
    Diagnostic[] _diagnostics;

    void Because() => (_source, _diagnostics, _) = CommandKeyCompilation.Compile("ModelKeys", Models);

    [Fact] void should_emit_compilable_registrations() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_declare_the_type_of_the_id() => _source.ShouldContain(".DeclareModelKey(typeof(global::Models.WithId), typeof(global::System.Guid))");
    [Fact] void should_declare_an_explicit_null_when_there_is_no_id() => _source.ShouldContain(".DeclareModelKey(typeof(global::Models.WithoutId), null)");
    [Fact] void should_not_see_a_non_public_id() => _source.ShouldContain(".DeclareModelKey(typeof(global::Models.WithHiddenId), null)");
}

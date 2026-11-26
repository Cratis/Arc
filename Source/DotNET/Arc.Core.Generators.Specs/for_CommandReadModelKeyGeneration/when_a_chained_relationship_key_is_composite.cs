// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_CommandReadModelKeyGeneration;

/// <summary>
/// Verifies generated coverage for relationship-only composite keys.
/// </summary>
public class when_a_chained_relationship_key_is_composite : Specification
{
    const string Models = CompositeKeyCompilation.Registry + """
        namespace Cratis.Arc.Chronicle.ReadModels
        {
            public static class ChainedReadModelKeys
            {
                public static void RegisterModel(System.Type model, params CommandReadModelKeyProperty[] properties) { }
            }
        }
        namespace CompositeChains
        {
            public record NestedKey(string Part);
            public record CompositeKey(string Part, NestedKey Nested);
            public record Source(System.Guid Id, CompositeKey Next);
            public record Target(CompositeKey Id);
            [Cratis.Arc.Commands.ModelBound.Command]
            public record Change(System.Guid Root)
            {
                public void Handle([Cratis.Arc.Commands.LookUpBy(nameof(source), nameof(Source.Next))] Target target, Source source) { }
            }
        }
        """;

    string _source;
    string _accessors;
    Diagnostic[] _diagnostics;

    void Because()
    {
        Compilation output;
        (_source, _diagnostics, output) = CommandKeyCompilation.Compile("CompositeChainedLookup", Models);
        _accessors = output.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("ProtectedReadModelKeyAccessors.g.cs", StringComparison.Ordinal)).ToString();
    }

    [Fact] void should_compile_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_relationship_getter() => _source.ShouldContain("((global::CompositeChains.Source)instance).@Next");
    [Fact] void should_cover_the_relationship_composite_not_present_on_the_command() => _accessors.ShouldContain(".Register<global::CompositeChains.CompositeKey>(");
    [Fact] void should_declare_the_relationship_key_for_the_command() => _accessors.ShouldContain(".DeclareKeyTypes(typeof(global::CompositeChains.Change), typeof(global::CompositeChains.CompositeKey))");
    [Fact] void should_cover_nested_key_parts() => _accessors.ShouldContain(".Register<global::CompositeChains.NestedKey>(");
    [Fact] void should_read_the_composite_parts_through_typed_accessors() => _accessors.ShouldContain("((global::CompositeChains.CompositeKey)instance).@Nested");
}

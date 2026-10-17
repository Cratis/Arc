// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_selecting_query_roots : Specification
{
    string _source;
    Diagnostic[] _diagnostics;
    string _metadata;

    void Because()
    {
        var reference = ModelGraphWalkerCompilation.Library("SelectedBaseLibrary", "using Cratis.Arc.Queries.ModelBound; namespace Library; [ReadModel(RequireQueryAttribute = true)] public class SelectedBase { }", true);
        const string models = """
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            namespace Models;
            public class Order : Library.SelectedBase
            {
                [Query] public static Order ById(Included filter) => new();
                public static Order ById(Excluded helper) => new();
                public static Task BeginReplay(Excluded service) => Task.CompletedTask;
                internal static Order Internal(Excluded helper) => new();
                private static Order Private(Excluded helper) => new();
                protected static Order Protected(Excluded helper) => new();
                protected internal static Order ProtectedInternal(Excluded helper) => new();
                private protected static Order PrivateProtected(Excluded helper) => new();
            }
            [ReadModel] internal class Conventional
            {
                public static Conventional All(Included filter) => new();
                internal static Conventional Internal(Excluded helper) => new();
                private static Conventional Private(Excluded helper) => new();
                protected static Conventional Protected(Excluded helper) => new();
                protected internal static Conventional ProtectedInternal(Excluded helper) => new();
                private protected static Conventional PrivateProtected(Excluded helper) => new();
            }
            public record Included(string Term);
            public record Excluded(string Value);
            public record OrderByIdParameters(string Term);
            public record BeginReplayParameters(string Value);
            """;
        var (source, diagnostics, output) = ModelGraphWalkerCompilation.Compile("SelectedRoots", models, true, [new QueryMetadataGenerator()], reference);
        _source = source;
        _diagnostics = diagnostics;
        _metadata = output.SyntaxTrees.Single(_ => _.FilePath.EndsWith("GeneratedQueryMetadata.g.cs", StringComparison.Ordinal)).ToString();
    }

    [Fact] void should_keep_public_queries_on_internal_models() => _metadata.ShouldContain("Models.Conventional.All");
    [Fact] void should_exclude_nonpublic_queries_from_metadata() => _metadata.ShouldNotContain("Models.Conventional.Internal");
    [Fact] void should_generate_metadata_from_the_referenced_setting() => _metadata.ShouldContain("Models.Order.ById");
    [Fact] void should_not_generate_metadata_for_the_unmarked_task_hook() => _metadata.ShouldNotContain("Models.Order.BeginReplay");
    [Fact] void should_emit_compilable_walkers() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_opt_in_from_a_referenced_assembly() => _source.ShouldContain("Register(typeof(global::Models.Included)");
    [Fact] void should_ignore_the_unmarked_overload_and_task_helper() => _source.ShouldNotContain("global::Models.Excluded");
    [Fact] void should_include_selected_query_parameter_models() => _source.ShouldContain("Register(typeof(global::Models.OrderByIdParameters)");
    [Fact] void should_not_include_unmarked_helper_parameter_models() => _source.ShouldNotContain("global::Models.BeginReplayParameters");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator;

public class when_selecting_public_queries : Specification
{
    GeneratorDriverRunResult _result;
    string _metadata;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;
            [ReadModel] internal class Order
            {
                public static Order Public() => new();
                internal static Order Internal() => new();
                private static Order Private() => new();
                protected static Order Protected() => new();
                protected internal static Order ProtectedInternal() => new();
                private protected static Order PrivateProtected() => new();
            }
            """);
        _metadata = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
    }

    [Fact] void should_accept_unmarked_nonpublic_helpers() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_a_public_query_on_an_internal_model() => _metadata.ShouldContain("\"Order.Public\"");
    [Fact] void should_not_emit_any_nonpublic_method() => _metadata.Split("QueryMetadataRegistry.Register(", StringSplitOptions.None).Length.ShouldEqual(2);
}

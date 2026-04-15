// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_without_read_models;

public class and_read_model_exists_with_no_valid_query_methods : Specification
{
    GeneratorDriverRunResult _result;
    string _generatedSource;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;

            namespace TestApp;

            [ReadModel]
            public class MyReadModel
            {
                public string Value { get; set; } = string.Empty;
            }
            """);

        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "CratisArcGeneratedMarker.g.cs");
    }

    [Fact] void should_generate_only_marker_source() => _result.GeneratedTrees.Length.ShouldEqual(1);
    [Fact] void should_generate_marker_type() => _generatedSource.ShouldContain("public static class GeneratedMarker");
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.Length.ShouldEqual(0);
}

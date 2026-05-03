// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_is_internal : Specification
{
    GeneratorDriverRunResult _result;
    string _generatedSource;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;

            namespace TestApp;

            [ReadModel]
            internal class MyReadModel
            {
                internal static MyReadModel GetById(int id) => new();
            }
            """);

        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
    }

    [Fact] void should_generate_two_source_files() => _result.GeneratedTrees.Length.ShouldEqual(2);
    [Fact] void should_include_internal_read_model_query() => _generatedSource.ShouldContain("TestApp.MyReadModel.GetById");
    [Fact] void should_reference_internal_read_model_type() => _generatedSource.ShouldContain("typeof(global::TestApp.MyReadModel)");
}
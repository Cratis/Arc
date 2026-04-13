// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_has_non_public_query_method : Specification
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
                public static MyReadModel GetById(int id) => new();
                internal static MyReadModel GetByName(string name) => new();
            }
            """);

        _generatedSource = _result.GeneratedTrees.FirstOrDefault()?.ToString() ?? string.Empty;
    }

    [Fact] void should_generate_one_source_file() => _result.GeneratedTrees.Length.ShouldEqual(1);
    [Fact] void should_include_public_query() => _generatedSource.ShouldContain("TestApp.MyReadModel.GetById");
    [Fact] void should_not_include_internal_query() => _generatedSource.ShouldNotContain("TestApp.MyReadModel.GetByName");
}

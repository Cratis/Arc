// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_multiple_read_models_exist : Specification
{
    GeneratorDriverRunResult _result;
    string _generatedSource;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;
            using System.Collections.Generic;

            namespace TestApp;

            [ReadModel]
            public class FirstReadModel
            {
                public static FirstReadModel GetById(int id) => new();
            }

            [ReadModel]
            public class SecondReadModel
            {
                public static IEnumerable<SecondReadModel> GetAll() => [];
            }
            """);

        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
    }

    [Fact] void should_generate_two_source_files() => _result.GeneratedTrees.Length.ShouldEqual(2);
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.Length.ShouldEqual(0);
    [Fact] void should_include_first_read_model_query() => _generatedSource.ShouldContain("TestApp.FirstReadModel.GetById");
    [Fact] void should_include_second_read_model_query() => _generatedSource.ShouldContain("TestApp.SecondReadModel.GetAll");
}

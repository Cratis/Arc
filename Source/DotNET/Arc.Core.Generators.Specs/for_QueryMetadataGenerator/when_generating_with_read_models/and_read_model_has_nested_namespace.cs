// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_has_nested_namespace : Specification
{
    GeneratorDriverRunResult _result;
    string _generatedSource;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;

            namespace TestApp.Features.Sample;

            [ReadModel]
            public class MyReadModel
            {
                public static MyReadModel GetById(int id) => new();
            }
            """);

        _generatedSource = _result.GeneratedTrees.FirstOrDefault()?.ToString() ?? string.Empty;
    }

    [Fact] void should_generate_one_source_file() => _result.GeneratedTrees.Length.ShouldEqual(1);
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.Length.ShouldEqual(0);
    [Fact] void should_use_global_qualified_type_reference() => _generatedSource.ShouldContain("typeof(global::TestApp.Features.Sample.MyReadModel)");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_query_contains_local_function : Specification
{
    GeneratorDriverRunResult _result;
    string _generatedSource;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;
            using System.Runtime.CompilerServices;

            namespace TestApp;

            [ReadModel]
            public class MyReadModel
            {
                public static MyReadModel GetById(int id)
                {
                    static MyReadModel Local() => new();
                    return Local();
                }

                [CompilerGenerated]
                public static MyReadModel Generated() => new();
            }
            """);
        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
    }

    [Fact] void should_include_only_the_public_query() => _generatedSource.ShouldContain("TestApp.MyReadModel.GetById");
    [Fact] void should_not_include_a_compiler_generated_method() => _generatedSource.ShouldNotContain("TestApp.MyReadModel.Generated");
}

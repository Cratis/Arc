// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_is_a_value_type_with_an_observable_query : Specification
{
    string _generatedSource;

    void Because()
    {
        var result = GeneratorTestHelper.RunGenerator("""
            using Cratis.Arc.Queries.ModelBound;
            using System;
            using System.Reactive.Subjects;

            namespace TestApp;

            [ReadModel]
            public record struct Counter(int Value)
            {
                public static ISubject<Counter> Observe() => throw new NotImplementedException();
            }
            """);
        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(result, "GeneratedQueryMetadata.g.cs");
    }

    [Fact] void should_register_the_value_type() => _generatedSource.ShouldContain("ObservableQueryElementTypes.Register<global::TestApp.Counter>();");
}

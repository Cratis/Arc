// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_has_observable_queries : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using System;
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using System.Threading.Tasks;

        namespace TestApp;

        [ReadModel]
        public class MyReadModel
        {
            public static ISubject<MyReadModel> One() => throw new NotImplementedException();
            public static ISubject<IEnumerable<MyReadModel>> All() => throw new NotImplementedException();
            public static IAsyncEnumerable<MyReadModel> Stream() => throw new NotImplementedException();
            public static Task<ISubject<MyReadModel[]>> Later() => throw new NotImplementedException();
            public static MyReadModel GetById(int id) => new();
        }
        """;

    GeneratorDriverRunResult _result;
    string _generatedSource;
    int _readModelRegistrations;
    IEnumerable<Diagnostic> _errors;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator(Source);
        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
        _readModelRegistrations = _generatedSource.Split("ObservableQueryElementTypes.Register<global::TestApp.MyReadModel>();").Length - 1;
        _errors = GeneratorTestHelper.CompileErrors(Source);
    }

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.Length.ShouldEqual(0);
    [Fact] void should_register_the_read_model_once() => _readModelRegistrations.ShouldEqual(1);
    [Fact] void should_register_the_enumerable_element_type() => _generatedSource.ShouldContain("ObservableQueryElementTypes.Register<global::System.Collections.Generic.IEnumerable<global::TestApp.MyReadModel>>();");
    [Fact] void should_register_the_array_element_type() => _generatedSource.ShouldContain("ObservableQueryElementTypes.Register<global::TestApp.MyReadModel[]>();");
    [Fact] void should_compile() => _errors.ShouldBeEmpty();
}

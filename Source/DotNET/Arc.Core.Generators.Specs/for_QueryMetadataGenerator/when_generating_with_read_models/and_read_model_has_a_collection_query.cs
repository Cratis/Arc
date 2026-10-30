// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Generators.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.for_QueryMetadataGenerator.when_generating_with_read_models;

public class and_read_model_has_a_collection_query : Specification
{
    const string Source = """
        using Cratis.Arc.Queries.ModelBound;
        using System;
        using System.Collections.Generic;

        namespace TestApp;

        public class BaseModel
        {
            public Guid Id { get; set; }
            public static decimal BaseStatic { get; }
            protected long Hidden { get; set; }
        }

        [ReadModel]
        public class MyReadModel : BaseModel
        {
            short _s;
            public string Name { get; set; }
            public int Age { get; set; }
            public int Rank { get; set; }
            public int? Score { get; set; }
            public static DateTime Created { get; }
            private byte Secret { get; set; }
            public char this[int i] => 'a';
            public ref short Counter => ref _s;

            public static IEnumerable<MyReadModel> All() => throw new NotImplementedException();
        }
        """;

    GeneratorDriverRunResult _result;
    string _generatedSource;
    IEnumerable<Diagnostic> _errors;

    void Because()
    {
        _result = GeneratorTestHelper.RunGenerator(Source);
        _generatedSource = GeneratorTestHelper.GetGeneratedSourceByHintName(_result, "GeneratedQueryMetadata.g.cs");
        _errors = GeneratorTestHelper.CompileErrors(Source);
    }

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.Length.ShouldEqual(0);
    [Fact] void should_register_the_read_model_once() => (_generatedSource.Split("QueryableElementTypes.Register<global::TestApp.MyReadModel>();").Length - 1).ShouldEqual(1);
    [Fact] void should_register_the_string_sort_key() => _generatedSource.ShouldContain("QueryableElementTypes.RegisterSortKey<global::TestApp.MyReadModel, string>();");
    [Fact] void should_register_the_int_sort_key_once() => (_generatedSource.Split("QueryableElementTypes.RegisterSortKey<global::TestApp.MyReadModel, int>();").Length - 1).ShouldEqual(1);
    [Fact] void should_register_the_nullable_sort_key() => _generatedSource.ShouldContain("QueryableElementTypes.RegisterSortKey<global::TestApp.MyReadModel, int?>();");
    [Fact] void should_register_the_inherited_sort_key() => _generatedSource.ShouldContain("QueryableElementTypes.RegisterSortKey<global::TestApp.MyReadModel, global::System.Guid>();");
    [Fact] void should_register_the_static_sort_key() => _generatedSource.ShouldContain("QueryableElementTypes.RegisterSortKey<global::TestApp.MyReadModel, global::System.DateTime>();");
    [Fact] void should_not_register_an_inherited_static_sort_key() => _generatedSource.ShouldNotContain("decimal>");
    [Fact] void should_not_register_a_protected_sort_key() => _generatedSource.ShouldNotContain("long>");
    [Fact] void should_not_register_a_private_sort_key() => _generatedSource.ShouldNotContain("byte>");
    [Fact] void should_not_register_an_indexer_sort_key() => _generatedSource.ShouldNotContain("char>");
    [Fact] void should_not_register_a_ref_returning_sort_key() => _generatedSource.ShouldNotContain("short>");
    [Fact] void should_compile() => _errors.ShouldBeEmpty();
}

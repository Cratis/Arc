// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// Query result optionality distinguishes the result from elements carried by a collection.
/// </summary>
public class with_query_return_optionality : Specification
{
    [Theory]
    [InlineData("Author?")]
    [InlineData("Task<Author?>")]
    [InlineData("ValueTask<Author?>")]
    void should_preserve_nullable_value_type_results(string returnType)
    {
        var source = $$"""
            using System;
            using System.Threading.Tasks;
            namespace Library.Authors.Listing;
            public readonly record struct Author(Guid Id, string Name)
            {
                public static {{returnType}} ById(Guid id) => default;
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        var author = compilation.GetTypeByMetadataName("Library.Authors.Listing.Author")!;
        var method = QueryReader.MethodsOf(author).Single();
        var result = new QueryReader(new TypeRegistry(), new ScreenplayDiagnostics()).Read(method, author, Analyzed.SlicePath)!;
        result.ReturnType.Name.ShouldEqual("Author");
        result.ReturnType.IsOptional.ShouldBeTrue();
        result.ReturnType.IsCollection.ShouldBeFalse();
        result.ReturnTypeFullName.ShouldEqual("Library.Authors.Listing.Author");
    }

    [Theory]
    [InlineData("Author?", "Author optional", false)]
    [InlineData("Task<Author?>", "Author optional", false)]
    [InlineData("ValueTask<Author?>", "Author optional", false)]
    [InlineData("Author[]?", "Author[] optional", false)]
    [InlineData("Task<Author[]?>", "Author[] optional", false)]
    [InlineData("IEnumerable<Author>?", "Author[] optional", false)]
    [InlineData("IAsyncEnumerable<Author>?", "Author[] optional", false)]
    [InlineData("Author?[]", "Author[]", true)]
    [InlineData("Task<Author?[]>", "Author[]", true)]
    void should_distinguish_an_absent_result_from_nullable_collection_elements(string returnType, string expected, bool nullableElements)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            namespace Library.Authors.Listing;
            [ReadModel]
            public record Author(Guid Id, string Name)
            {
                public static {{returnType}} ById(Guid id) => default!;
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        var result = new ScreenplayGenerator().Generate(Analyzed.Compile((Analyzed.SlicePath, source)), new());
        result.Source.Split('\n').Any(line => line.Trim() == $"query ById => {expected}").ShouldBeTrue();
        result.Diagnostics.Any(d => d.Code == "SP0019" && d.Message.Contains("nullable collection elements", StringComparison.Ordinal)).ShouldEqual(nullableElements);
        new ScreenplayCompiler().Compile(result.Source).Diagnostics.ShouldBeEmpty();
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
        if (expected == "Author optional")
        {
            new ScreenplayVerifier().Verify(result.Source).BindingDiagnostics.Where(d => d.Severity != Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Information).ShouldBeEmpty();
        }
    }
}

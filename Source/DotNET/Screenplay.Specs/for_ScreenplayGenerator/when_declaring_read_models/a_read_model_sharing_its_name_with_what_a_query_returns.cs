// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model sharing its simple name with a type a controller query answers with, which is no read model at all.
/// The query is written as answering with that name, so declaring the read model would bind the query - and the
/// identity it implies - to a shape it never returns. The read model is not declared.
/// </summary>
public class a_read_model_sharing_its_name_with_what_a_query_returns : a_read_model_document
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;
        """;

    const string Listing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Catalog.Listing;

        [ReadModel]
        public record Book(string Title)
        {
            public static IEnumerable<Book> AllBooks() => [];
        }
        """;

    const string Browsing = """
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Shelves.Browsing;

        public record Book(string Isbn, int Shelf);

        public class ShelfController : ControllerBase
        {
            [HttpGet]
            public Book? ByIsbn(string isbn) => null;
        }
        """;

    void Because() => Generate(
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Catalog/Listing/Listing.cs", Listing),
        ("Library/Shelves/Browsing/ShelfController.cs", Browsing));

    [Fact] void should_not_declare_the_read_model() => Count("readmodel Book").ShouldEqual(0);
    [Fact] void should_say_why() => LeftOut.Single().ShouldContain("'Library.Shelves.Browsing.Book'");
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}

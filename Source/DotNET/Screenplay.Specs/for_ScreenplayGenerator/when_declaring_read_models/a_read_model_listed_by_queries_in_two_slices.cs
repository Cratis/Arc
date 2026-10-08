// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model written outside every slice, which nothing builds or reads by a key, listed by queries in two slices.
/// A slice declaring a query onto it declares it, and of the two the first in namespace order does.
/// </summary>
public class a_read_model_listed_by_queries_in_two_slices : a_read_model_document
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;
        """;

    const string Writing = """
        namespace Library.Shared;

        [Cratis.Arc.Queries.ModelBound.ReadModel]
        public record Thing(string Name);
        """;

    const string Beta = """
        using System.Collections.Generic;
        using Library.Shared;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Shelves.Beta;

        public class BetaController : ControllerBase
        {
            [HttpGet]
            public IEnumerable<Thing> BetaThings() => [];
        }
        """;

    const string Alpha = """
        using System.Collections.Generic;
        using Library.Shared;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Shelves.Alpha;

        public class AlphaController : ControllerBase
        {
            [HttpGet]
            public IEnumerable<Thing> AlphaThings() => [];
        }
        """;

    void Because() => Generate(
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Shared/Thing.cs", Writing),
        ("Library/Shelves/Beta/BetaController.cs", Beta),
        ("Library/Shelves/Alpha/AlphaController.cs", Alpha));

    IEnumerable<string> DeclaredIn(string @namespace) =>
        Result.Model.Slices.Single(_ => _.Namespace == @namespace).ReadModels.Select(_ => _.Name);

    [Fact] void should_declare_it_in_the_first_slice_in_namespace_order() => DeclaredIn("Library.Shelves.Alpha").ShouldContainOnly(["Thing"]);
    [Fact] void should_not_declare_it_in_the_other() => DeclaredIn("Library.Shelves.Beta").ShouldBeEmpty();
    [Fact] void should_declare_it_once() => Count("readmodel Thing").ShouldEqual(1);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}

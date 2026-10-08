// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model nothing reads by a key and nothing builds, written in a namespace that is a slice of its own, and
/// listed by a query in a slice earlier in namespace order. The slice it is written in declares it.
/// </summary>
public class a_read_model_only_listed_by_a_query_in_another_slice : a_read_model_document
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;
        """;

    const string Writing = """
        using Cratis.Chronicle.Events;

        namespace Library.Shelves.Zeta;

        [EventType]
        public record ThingNoted(string Name);

        [Cratis.Arc.Queries.ModelBound.ReadModel]
        public record Thing(string Name);
        """;

    const string Listing = """
        using System.Collections.Generic;
        using Library.Shelves.Zeta;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Shelves.Alpha;

        public class ThingsController : ControllerBase
        {
            [HttpGet]
            public IEnumerable<Thing> AllThings() => [];
        }
        """;

    void Because() => Generate(
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Shelves/Zeta/Thing.cs", Writing),
        ("Library/Shelves/Alpha/ThingsController.cs", Listing));

    IEnumerable<string> DeclaredIn(string @namespace) =>
        Result.Model.Slices.Single(_ => _.Namespace == @namespace).ReadModels.Select(_ => _.Name);

    [Fact] void should_declare_it_in_the_slice_it_is_written_in() => DeclaredIn("Library.Shelves.Zeta").ShouldContainOnly(["Thing"]);
    [Fact] void should_not_declare_it_beside_the_query() => DeclaredIn("Library.Shelves.Alpha").ShouldBeEmpty();
    [Fact] void should_declare_it_once() => Count("readmodel Thing").ShouldEqual(1);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}

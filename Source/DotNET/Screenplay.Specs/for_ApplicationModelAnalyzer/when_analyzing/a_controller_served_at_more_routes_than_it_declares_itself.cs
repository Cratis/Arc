// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A controller answers at every template that reaches it, not only at the first one written on it: the route and verb
/// attributes may each be applied more than once, and a template on an abstract base controller serves the controllers
/// deriving from it. Each of those is a route the application really serves, and stopping at the first would pass the
/// rest over in silence.
/// </summary>
/// <remarks>
/// The web framework is declared alongside the application rather than referenced, the same way the other controller
/// specifications do it, because the recognizer matches on names rather than on the assembly they came from. It allows
/// the route and verb attributes to be applied more than once, as ASP.NET Core's own do.
/// </remarks>
public class a_controller_served_at_more_routes_than_it_declares_itself : Specification
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public sealed class RouteAttribute(string template) : Attribute
        {
            public string Template { get; } = template;
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public sealed class HttpGetAttribute(string template = "") : Attribute
        {
            public string Template { get; } = template;
        }
        """;

    const string Source = """
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Authors.Listing;

        public record Author(string Id, string Name);

        [Route("catalog")]
        public abstract class CatalogController : ControllerBase;

        [Route("catalog/authors")]
        [Route("authors")]
        public class AuthorsController : CatalogController
        {
            [HttpGet("all")]
            [HttpGet("everything")]
            public IEnumerable<Author> All() => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(
        ("Framework.cs", WebFramework),
        ("Library/Authors/Listing/Listing.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Framework.cs", WebFramework), ("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_query() => _analysis.Slice().Queries.Single().Name.ShouldEqual("All");
    [Fact] void should_say_where_the_controller_answers() => _analysis.Diagnostics.Count(_ => _.Message.Contains("catalog/authors'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_also_say_the_second_route_on_the_controller() => _analysis.Diagnostics.Count(_ => _.Message.Contains("'authors'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_also_say_the_route_it_inherits() => _analysis.Diagnostics.Count(_ => _.Message.Contains("'catalog'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_both_routes_the_query_answers_at() => _analysis.Diagnostics.Count(_ => _.Message.Contains("'all'", StringComparison.Ordinal) || _.Message.Contains("'everything'", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_report_every_route_as_a_serving_concern() => _analysis.Diagnostics.All(_ => _.Code == ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart).ShouldBeTrue();
}

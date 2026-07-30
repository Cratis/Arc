// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A controller says where it answers twice over - once for the controller and once for each verb that carries a
/// template. The document describes neither, and says so for each, so a reader can tell a route that was passed over
/// from an application that leaves the convention alone.
/// </summary>
/// <remarks>
/// The web framework is declared alongside the application rather than referenced, the same way the other controller
/// specifications do it, because the recognizer matches on names rather than on the assembly they came from.
/// </remarks>
public class a_controller_served_at_a_route_of_its_own : Specification
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class RouteAttribute(string template) : Attribute
        {
            public string Template { get; } = template;
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpPostAttribute(string template = "") : Attribute
        {
            public string Template { get; } = template;
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute(string template = "") : Attribute
        {
            public string Template { get; } = template;
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        public sealed class FromBodyAttribute : Attribute;
        """;

    const string Source = """
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        public record RegisterAuthor(string Name);

        public record Author(string Id, string Name);

        [Route("catalog/authors")]
        public class AuthorsController : ControllerBase
        {
            [HttpPost("register-author")]
            public Task<AuthorRegistered> Register([FromBody] RegisterAuthor command) =>
                Task.FromResult(new AuthorRegistered(command.Name));

            [HttpGet]
            public IEnumerable<Author> All() => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(
        ("Framework.cs", WebFramework),
        ("Library/Authors/Registration/Registration.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Framework.cs", WebFramework), ("Library/Authors/Registration/Registration.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_command() => _analysis.Slice().Commands.Single().Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_name_the_command_the_way_the_document_does() => _analysis.Diagnostics.Single(_ => _.Message.Contains("register-author'", StringComparison.Ordinal)).Message.ShouldContain("'RegisterAuthor'");
    [Fact] void should_still_recover_the_query() => _analysis.Slice().Queries.Single().Name.ShouldEqual("All");
    [Fact] void should_say_where_the_controller_answers() => _analysis.Diagnostics.Count(_ => _.Message.Contains("catalog/authors'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_where_the_command_answers() => _analysis.Diagnostics.Count(_ => _.Message.Contains("register-author'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_nothing_about_the_verb_that_leaves_the_convention_alone() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart).ShouldEqual(2);
}

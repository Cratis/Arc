// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Not every application is model-bound. A controller method that changes state is a command whose shape is its
/// request body, and one that reads state is a query - a whole category of application that was previously invisible.
/// </summary>
/// <remarks>
/// The web framework is declared alongside the application rather than referenced, because the recognizer matches on
/// the names of the base type and the verb attributes and never on the assembly they came from.
/// </remarks>
public class a_controller_exposing_commands_and_queries : Specification
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpPostAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Parameter)]
        public sealed class FromBodyAttribute : Attribute;
        """;

    const string Source = """
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Cratis.Arc.Authorization;
        using Cratis.Chronicle.Events;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        public record RegisterAuthor(string Name, int Age);

        public record Author(string Id, string Name);

        public class AuthorsController : ControllerBase
        {
            /// <summary>
            /// Registers a new author.
            /// </summary>
            [HttpPost]
            [Roles("Librarian")]
            public Task<AuthorRegistered> Register([FromBody] RegisterAuthor command) =>
                Task.FromResult(new AuthorRegistered(command.Name));

            [HttpGet]
            public IEnumerable<Author> AllAuthors() => [];

            [HttpGet]
            public Author AuthorById(string id) => new(id, string.Empty);
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Authors/Registration/AuthorsController.cs", Source)
    ];

    ApplicationModelAnalysis _analysis;
    SliceModel _slice;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _slice = _analysis.Model.Slices.First(_ => _.Namespace == "Library.Authors.Registration");
    }

    QueryModel Query(string name) => _slice.Queries.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_name_the_command_after_its_request_body() => _slice.Commands.Single().Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_take_the_input_from_the_request_body() => _slice.Commands.Single().Properties.Select(_ => _.Name).ShouldContainOnly(["Name", "Age"]);
    [Fact] void should_take_the_description_from_the_method() => _slice.Commands.Single().Description.ShouldEqual("Registers a new author.");
    [Fact] void should_read_what_the_method_requires_of_the_caller() => _slice.Commands.Single().Authorization!.Roles.ShouldContainOnly(["Librarian"]);
    [Fact] void should_produce_the_event_the_method_constructs() => _slice.Commands.Single().Produces.Single().EventName.ShouldEqual("AuthorRegistered");
    [Fact] void should_recover_both_queries() => _slice.Queries.Select(_ => _.Name).ShouldContainOnly(["AllAuthors", "AuthorById"]);
    [Fact] void should_return_many_from_the_listing_query() => Query("AllAuthors").ReturnType.ShouldEqual(new TypeReferenceModel("Author", true, false));
    [Fact] void should_identify_an_instance_by_the_required_parameter() => Query("AuthorById").By!.Name.ShouldEqual("id");
    [Fact] void should_infer_a_state_change_slice() => _slice.Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_point_at_the_file_the_controller_lives_in() => _slice.Commands.Single().SourceFilePath.ShouldEqual("Authors/Registration/AuthorsController.cs");
}

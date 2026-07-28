// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The other half of the promise: an application written against controllers rather than model-bound artifacts has
/// to come out of the same generator as a document the real Screenplay compiler accepts without a single
/// diagnostic. This is the shape that produced the defects worth fixing - transport types standing in for read
/// models, and two queries in one slice under one name - so it is the shape worth gating on.
/// </summary>
public class from_the_source_of_a_controller_based_application : Specification
{
    const string WebFramework = """
        using System;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        public interface IActionResult;

        public abstract class ActionResult : IActionResult;

        public sealed class OkObjectResult : ActionResult;

        public sealed class ActionResult<TValue>
        {
            public static implicit operator ActionResult<TValue>(TValue value) => new();
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpPostAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Parameter)]
        public sealed class FromBodyAttribute : Attribute;
        """;

    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Authorization;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Messaging.Feed;

        public record MessageText(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record MessagePosted(MessageText Text);

        public record PostToFeed(MessageText Text);

        public record FeedMessage(string Id, MessageText Text);

        public record ArchivedMessage(string Id, MessageText Text);

        public class FeedController : ControllerBase
        {
            /// <summary>
            /// Posts a message to the feed.
            /// </summary>
            [HttpPost]
            [Roles("Author")]
            public MessagePosted Post([FromBody] PostToFeed command) => new(command.Text);

            [HttpGet]
            public ActionResult<IEnumerable<FeedMessage>> ObserveAll() => new();

            [HttpGet]
            public IActionResult Raw() => new OkObjectResult();
        }

        public class ArchiveController : ControllerBase
        {
            [HttpGet]
            public IEnumerable<ArchivedMessage> ObserveAll() => [];
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Messaging/Feed/FeedController.cs", Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_the_real_compiler_accepts() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_real_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_declare_the_command_named_after_its_request_body() => Says("command PostToFeed").ShouldBeTrue();
    [Fact] void should_state_what_the_command_produces() => Says("produces MessagePosted").ShouldBeTrue();
    [Fact] void should_declare_what_the_command_requires_of_the_caller() => Says("authorize Author").ShouldBeTrue();
    [Fact] void should_tell_the_two_observing_queries_apart() => Says("query ArchiveControllerObserveAll").ShouldBeTrue();
    [Fact] void should_tell_the_other_one_apart_too() => Says("query FeedControllerObserveAll").ShouldBeTrue();
    [Fact] void should_never_put_a_transport_type_in_the_document() => Says("ActionResult").ShouldBeFalse();
    [Fact] void should_leave_out_the_query_whose_read_model_is_unknowable() => Says("query Raw").ShouldBeFalse();
    [Fact] void should_report_the_query_it_left_out() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableQuery);
    [Fact] void should_report_nothing_as_an_error() => _result.IsSuccess.ShouldBeTrue();
}

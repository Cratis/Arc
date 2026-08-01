// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A controller method's return type says two different things at once - what was read, and how it is carried back.
/// Only the first belongs in a document. A result that still carries its value is unwrapped; one that has thrown the
/// value away at the type level cannot be recovered by anything, so the query is left out and reported rather than
/// emitted with a type from the web framework standing in for a read model.
/// </summary>
public class a_controller_returning_a_transport_type : Specification
{
    const string WebFramework = """
        using System;
        using System.Collections.Generic;

        namespace Microsoft.AspNetCore.Mvc;

        public abstract class ControllerBase;

        public interface IActionResult;

        public abstract class ActionResult : IActionResult;

        public sealed class JsonResult : ActionResult;

        public sealed class ActionResult<TValue>
        {
            public static implicit operator ActionResult<TValue>(TValue value) => new();
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class HttpGetAttribute : Attribute;
        """;

    const string Source = """
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Mvc;

        namespace Library.Messaging.Feed;

        public record Message(string Text);

        public class MessagesController : ControllerBase
        {
            [HttpGet]
            public ActionResult<IEnumerable<Message>> Carried() => new();

            [HttpGet]
            public ActionResult Bare() => new JsonResult();

            [HttpGet]
            public IActionResult BareInterface() => new JsonResult();

            [HttpGet]
            public JsonResult Derived() => new();

            [HttpGet]
            public IEnumerable<Message> Plain() => [];
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Web/Mvc.cs", WebFramework),
        ("Library/Messaging/Feed/MessagesController.cs", Source)
    ];

    ApplicationModelAnalysis _analysis;
    SliceModel _slice;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _slice = _analysis.Model.Slices.First(_ => _.Namespace == "Library.Messaging.Feed");
    }

    QueryModel Query(string name) => _slice.Queries.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_keep_only_the_queries_whose_read_model_is_knowable() => _slice.Queries.Select(_ => _.Name).ShouldContainOnly(["Carried", "Plain"]);
    [Fact] void should_unwrap_a_result_that_still_carries_its_value() => Query("Carried").ReturnType.ShouldEqual(new TypeReferenceModel("Message", true, false));
    [Fact] void should_leave_a_plain_return_type_alone() => Query("Plain").ReturnType.ShouldEqual(new TypeReferenceModel("Message", true, false));
    [Fact] void should_never_emit_a_transport_type_as_a_read_model() => _slice.Queries.Any(_ => _.ReturnType.Name.Contains("ActionResult", StringComparison.Ordinal) || _.ReturnType.Name == "JsonResult").ShouldBeFalse();
    [Fact] void should_report_every_query_it_left_out() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableQuery).ShouldEqual(3);
    [Fact] void should_say_what_was_returned_instead() => _analysis.Diagnostics.Any(_ => _.Message.Contains("says how the result is transported", StringComparison.Ordinal)).ShouldBeTrue();
}

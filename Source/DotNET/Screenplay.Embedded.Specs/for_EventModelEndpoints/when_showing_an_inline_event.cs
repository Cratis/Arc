// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored document is explicitly normalized to LF before it is parsed.")]
public class when_showing_an_inline_event : Specification
{
    EventModelView _view;
    Slice _producer;
    Slice _consumer;

    void Because()
    {
        var source = """
            domain Library
            module Catalog
              feature Authors
                slice StateView Listing
                  readmodel Authors
                    name String
                  projection Authors => Authors
                    from AuthorRegistered
                      name = name
                slice StateChange Registration
                  command RegisterAuthor
                    id Uuid identifier
                    name String
                    produces event AuthorRegistered
                      tag audit
                      for id
                      name String = name
            """.ReplaceLineEndings("\n");
        _view = new EventModelParser().Parse("Library", "Library", source);
        var slices = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices;
        _producer = slices.Single(_ => _.Name == "Registration");
        _consumer = slices.Single(_ => _.Name == "Listing");
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_show_the_inline_event_once() => _producer.Events.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_event_schema() => _producer.Events[0].Schema["properties"]!["name"]!["type"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_preserve_event_tags() => _producer.Events[0].Tags.Single().ShouldEqual("audit");
    [Fact] void should_use_the_declaration_identity() => _producer.Events[0].Id.ShouldEqual(DeterministicId.From("Library", "Catalog/Authors/Registration", "event", "AuthorRegistered"));
    [Fact] void should_link_a_consumer_visited_before_the_producer() => _consumer.Events.Single().SourceEventId.ShouldEqual(_producer.Events[0].Id.ToString());
    [Fact] void should_carry_the_schema_to_the_consumer() => _consumer.Events.Single().Schema["properties"]!["name"]!["type"]!.GetValue<string>().ShouldEqual("string");
}

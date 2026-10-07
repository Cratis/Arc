// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored document is explicitly normalized to LF before it is parsed.")]
public class when_showing_a_generated_identifier : Specification
{
    EventModelView _view;
    CommandItem _command;

    void Because()
    {
        var source = """
            domain Library
            concept AuthorId : Uuid
            module Catalog
              feature Authors
                slice StateChange Registration
                  command RegisterAuthor
                    id AuthorId generated identifier
                    name String
                    returns id
                    produces event AuthorRegistered
                      for id
                      name String = name
            """.ReplaceLineEndings("\n");
        _view = new EventModelParser().Parse("Library", "Library", source);
        _view.Errors.ShouldBeEmpty();
        _command = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices[0].Command!;
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_not_require_the_generated_identifier() => _command.Schema["required"]!.AsArray().Select(value => value!.GetValue<string>()).ShouldNotContain("id");
    [Fact] void should_not_offer_the_generated_identifier_as_an_input() => _command.Schema["properties"]!.AsObject().ContainsKey("id").ShouldBeFalse();
    [Fact] void should_preserve_the_request_input() => _command.Schema["properties"]!["name"]!["type"]!.GetValue<string>().ShouldEqual("string");
}

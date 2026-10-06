// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored document is explicitly normalized to LF before it is parsed.")]
public class when_showing_computed_event_tags : Specification
{
    EventModelView _view;
    EventItem _event;

    void Because()
    {
        _view = new EventModelParser().Parse("Library", "Library", """
            domain Library
            module Catalog
              feature Authors
                slice StateChange Registration
                  event AuthorRegistered
                    tag $context.identity.id
                    tag $env.SERVICE_NAME
                    name String
            """.ReplaceLineEndings("\n"));
        _event = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices[0].Events[0];
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_preserve_context_and_environment_tags() => _event.Tags.ShouldEqual("$context.identity.id", "$env.SERVICE_NAME");
}

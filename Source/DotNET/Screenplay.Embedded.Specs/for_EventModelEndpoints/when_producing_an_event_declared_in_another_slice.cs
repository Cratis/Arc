// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_producing_an_event_declared_in_another_slice : Specification
{
    EventModelView _view;
    Slice _producer;

    void Because()
    {
        var source = string.Join(
            '\n',
            "domain Library",
            "module Catalog",
            "  feature Authors",
            "    slice StateChange Registration",
            "      command RegisterAuthor",
            "        name String",
            "        produces AuthorRegistered",
            "          name = name",
            "    slice StateChange Declarations",
            "      event AuthorRegistered",
            "        name String");
        _view = new EventModelParser().Parse("Library", "Library", source);
        _producer = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices[0];
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_show_the_produced_event_on_the_command_slice() => _producer.Events.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_carry_the_produced_event_schema() => _producer.Events[0].Schema["properties"]!["name"]!["type"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_not_falsely_warn_that_the_event_is_missing() => _view.Warnings.Any(_ => _.Message.Contains("events the command", StringComparison.Ordinal)).ShouldBeFalse();
}

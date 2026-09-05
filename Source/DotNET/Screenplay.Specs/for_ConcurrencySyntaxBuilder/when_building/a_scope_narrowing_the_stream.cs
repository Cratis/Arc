// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ConcurrencySyntaxBuilder.when_building;

public class a_scope_narrowing_the_stream : Specification
{
    ScreenplayDiagnostics _diagnostics;
    ConcurrencySyntaxBuilder _builder;
    ConcurrencySyntax? _result;

    void Establish()
    {
        _diagnostics = new();
        _builder = new(new ScreenplayNaming(), _diagnostics);
    }

    void Because() => _result = _builder.Build(
        new ConcurrencyModel(true, "Author", "Inventory", "Primary", ["BookAddedToInventory"]),
        "Library.Inventory.Adding");

    [Fact] void should_include_the_event_source() => _result!.EventSource.ShouldBeTrue();
    [Fact] void should_carry_the_source_type() => _result!.EventSourceType.ShouldEqual("Author");
    [Fact] void should_carry_the_stream_type() => _result!.EventStreamType.ShouldEqual("Inventory");
    [Fact] void should_carry_the_stream_id() => _result!.EventStreamId.ShouldEqual("Primary");
    [Fact] void should_carry_the_event_types() => _result!.EventTypes.ShouldContainOnly(["BookAddedToInventory"]);
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}

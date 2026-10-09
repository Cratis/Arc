// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_SpecificationSyntaxBuilder.when_building;

public class an_append_with_a_concrete_route : Specification
{
    SpecificationSyntax _syntax;

    void Because()
    {
        var appended = new SpecificationStateModel("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Jane Austen"))])
        {
            For = new("author"),
            Route = new("Account", "Transactions", new("October"))
        };
        var readModel = new SpecificationStateModel("Author", SpecificationStateKind.ReadModel, [new("Name", new LiteralSource("Jane Austen"))]);
        var specification = new SpecificationModel("Appending", [], appended, [readModel], []);
        _syntax = new SpecificationSyntaxBuilder(new ScreenplayNaming()) { AuthoringOnlyConstructs = true }.Build([specification]).Single();
    }

    [Fact]
    public void should_state_no_stream_only_on_an_expected_event()
    {
        var action = new SpecificationStateModel("RegisterAuthor", SpecificationStateKind.Command, []);
        var expected = new SpecificationStateModel("AuthorRegistered", SpecificationStateKind.Event, [new("Name", new LiteralSource("Jane Austen"))])
        {
            Route = SpecificationEventRouteModel.NoStream
        };
        var specification = new SpecificationModel("Registering", [], action, [expected], []);
        var syntax = new SpecificationSyntaxBuilder(new ScreenplayNaming()) { AuthoringOnlyConstructs = true }.Build([specification]).Single();
        syntax.ThenEvents.Single().NoStream.ShouldNotBeNull();
    }

    [Fact] void should_keep_the_append() => _syntax.WhenAppended!.EventType.ShouldEqual("AuthorRegistered");
    [Fact] void should_state_its_source() => _syntax.WhenAppended!.Stream!.EventSource.ShouldEqual("Account");
    [Fact] void should_state_its_stream() => _syntax.WhenAppended!.Stream!.Stream.ShouldEqual("Transactions");
    [Fact] void should_state_its_stream_id() => _syntax.WhenAppended!.Stream!.StreamId.ShouldNotBeNull();
    [Fact] void should_keep_the_following_assertion() => _syntax.ThenReadModels!.Single().Name.ShouldEqual("Author");
}

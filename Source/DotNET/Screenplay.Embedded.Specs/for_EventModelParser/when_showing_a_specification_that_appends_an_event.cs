// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_showing_a_specification_that_appends_an_event : Specification
{
    EventModelView _view;
    Slice _listing;
    SliceSpecification _specification;

    void Because()
    {
        var source = string.Join(
            '\n',
            "domain Library",
            "module Catalog",
            "  feature Authors",
            "    slice StateView Listing",
            "      readmodel Author",
            "        name String",
            "      projection Author => Author",
            "        automap",
            "        from AuthorRegistered",
            "      specification ListingARegisteredAuthor",
            "        when append AuthorRegistered",
            "          for \"11111111-1111-1111-1111-111111111111\"",
            "          name = \"Ursula\"",
            "        then readmodel Author",
            "          name = \"Ursula\"",
            "        then no readmodel Author for \"22222222-2222-2222-2222-222222222222\"",
            "    slice StateChange Registration",
            "      event AuthorRegistered",
            "        name String");
        _view = new EventModelParser().Parse("Library", "Library", source);
        var slices = _view.EventModel!.Collections[0].Modules[0].Features[0].Slices;
        _listing = slices.Single(_ => _.Name == "Listing");
        _specification = _listing.Specifications.Single();
    }

    [Fact] void should_compile_the_document() => _view.Success.ShouldBeTrue();
    [Fact] void should_name_the_action_after_the_appended_event_and_its_source() => _specification.When!.Name.ShouldEqual("append AuthorRegistered — for \"11111111-1111-1111-1111-111111111111\"");
    [Fact] void should_not_point_the_action_at_a_command() => _specification.When!.CommandId.ShouldBeNull();
    [Fact] void should_carry_the_appended_values() => _specification.When!.Values["name"]!.GetValue<string>().ShouldEqual("Ursula");
    [Fact] void should_name_the_expected_read_model() => _specification.Name.ShouldContain("then readmodel Author { name = \"Ursula\" }");
    [Fact] void should_name_the_read_model_expected_to_be_absent() => _specification.Name.ShouldContain("then no readmodel Author for \"22222222-2222-2222-2222-222222222222\"");
    [Fact] void should_expect_no_events() => _specification.ThenEvents.ShouldBeEmpty();
}

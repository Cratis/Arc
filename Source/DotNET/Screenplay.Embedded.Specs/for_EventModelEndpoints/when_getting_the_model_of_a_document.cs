// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_the_model_of_a_document : a_host
{
    HttpResponseMessage _response;
    JsonElement _view;
    JsonElement _eventModel;
    JsonElement _module;
    JsonElement _feature;
    JsonElement _slices;

    async Task Because()
    {
        _response = await _client.GetAsync(Url($"/documents/{an_embedded_application.ProjectId}/{an_embedded_application.ApplicationDocumentId}/model"));
        _view = JsonDocument.Parse(await _response.Content.ReadAsStringAsync()).RootElement;
        _eventModel = _view.GetProperty("eventModel");
        _module = _eventModel.GetProperty("collections")[0].GetProperty("modules")[0];
        _feature = _module.GetProperty("features")[0];
        _slices = _feature.GetProperty("slices");
    }

    [Fact] void should_answer_with_the_model() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_say_it_succeeded() => _view.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_no_errors() => _view.GetProperty("errors").GetArrayLength().ShouldEqual(0);
    [Fact] void should_identify_the_document_with_a_guid() => Guid.TryParse(_eventModel.GetProperty("id").GetString(), out _).ShouldBeTrue();
    [Fact] void should_name_the_document_after_the_domain() => _eventModel.GetProperty("name").GetString().ShouldEqual("Fixture");
    [Fact] void should_hold_a_single_collection() => _eventModel.GetProperty("collections").GetArrayLength().ShouldEqual(1);
    [Fact] void should_place_the_collection_at_origin() => _eventModel.GetProperty("collections")[0].GetProperty("position").GetProperty("x").GetDouble().ShouldEqual(0d);
    [Fact] void should_hold_no_actors_on_the_collection() => _eventModel.GetProperty("collections")[0].GetProperty("actors").GetArrayLength().ShouldEqual(0);
    [Fact] void should_hold_no_sticky_notes() => _eventModel.GetProperty("stickyNotes").GetArrayLength().ShouldEqual(0);
    [Fact] void should_hold_no_links() => _eventModel.GetProperty("links").GetArrayLength().ShouldEqual(0);
    [Fact] void should_carry_the_module() => _module.GetProperty("name").GetString().ShouldEqual("Catalog");
    [Fact] void should_carry_the_feature() => _feature.GetProperty("name").GetString().ShouldEqual("Books");
    [Fact] void should_carry_every_slice() => _slices.GetArrayLength().ShouldEqual(2);
    [Fact] void should_carry_the_state_change_slice_as_one() => _slices[0].GetProperty("sliceType").GetInt32().ShouldEqual(0);
    [Fact] void should_carry_the_state_view_slice_as_one() => _slices[1].GetProperty("sliceType").GetInt32().ShouldEqual(1);
    [Fact] void should_carry_the_command() => _slices[0].GetProperty("command").GetProperty("name").GetString().ShouldEqual("RegisterBook");
    [Fact] void should_carry_the_schema_of_the_command() => _slices[0].GetProperty("command").GetProperty("schema").GetProperty("properties").GetProperty("title").GetProperty("type").GetString().ShouldEqual("string");
    [Fact] void should_carry_the_validation_rules_of_the_command() => _slices[0].GetProperty("command").GetProperty("rules")[0].GetProperty("propertyName").GetString().ShouldEqual("title");
    [Fact] void should_carry_the_event() => _slices[0].GetProperty("events")[0].GetProperty("name").GetString().ShouldEqual("BookRegistered");
    [Fact] void should_carry_the_read_model_of_the_state_view_slice() => _slices[1].GetProperty("readModel").GetProperty("name").GetString().ShouldEqual("BookListReadModel");
    [Fact] void should_carry_the_query_of_the_state_view_slice() => _slices[1].GetProperty("queries")[0].GetProperty("name").GetString().ShouldEqual("ListBooks");
    [Fact] void should_carry_the_consumed_event_on_the_state_view_slice() => _slices[1].GetProperty("events")[0].GetProperty("name").GetString().ShouldEqual("BookRegistered");

    [Fact]
    void should_reference_the_producing_event_from_the_consumed_one() =>
        _slices[1].GetProperty("events")[0].GetProperty("sourceEventId").GetString()
            .ShouldEqual(_slices[0].GetProperty("events")[0].GetProperty("id").GetString());

    [Fact]
    void should_warn_about_the_screen_the_board_cannot_hold() =>
        _view.GetProperty("warnings").EnumerateArray()
            .Any(warning => warning.GetProperty("message").GetString()!.Contains("screen", StringComparison.Ordinal))
            .ShouldBeTrue();
}

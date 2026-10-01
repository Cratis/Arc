// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_getting_the_hierarchy : a_host
{
    HttpResponseMessage _response;
    JsonElement _projects;

    async Task Because()
    {
        _response = await _client.GetAsync(Url("/hierarchy"));
        _projects = JsonDocument.Parse(await _response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact] void should_answer_with_the_hierarchy() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_hold_one_project_per_assembly() => _projects.GetArrayLength().ShouldEqual(1);
    [Fact] void should_identify_the_project_by_the_assembly_name() => _projects[0].GetProperty("id").GetString().ShouldEqual(an_embedded_application.ProjectId);
    [Fact] void should_name_the_project_after_the_assembly() => _projects[0].GetProperty("name").GetString().ShouldEqual(an_embedded_application.ProjectId);
    [Fact] void should_hold_every_document_the_catalog_names() => _projects[0].GetProperty("documents").GetArrayLength().ShouldEqual(2);
    [Fact] void should_identify_a_document_by_its_namespace() => _projects[0].GetProperty("documents")[1].GetProperty("id").GetString().ShouldEqual(an_embedded_application.ModuleDocumentId);
    [Fact] void should_say_what_kind_of_node_the_root_document_is() => _projects[0].GetProperty("documents")[0].GetProperty("kind").GetString().ShouldEqual("assembly");
    [Fact] void should_say_what_kind_of_node_the_module_document_is() => _projects[0].GetProperty("documents")[1].GetProperty("kind").GetString().ShouldEqual("module");
    [Fact] void should_hold_no_parent_for_the_root_document() => _projects[0].GetProperty("documents")[0].GetProperty("parentId").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_hold_the_parent_of_the_module_document() => _projects[0].GetProperty("documents")[1].GetProperty("parentId").GetString().ShouldEqual(an_embedded_application.ApplicationDocumentId);
    [Fact] void should_say_which_resource_holds_the_source() => _projects[0].GetProperty("documents")[1].GetProperty("resourceName").GetString().ShouldEqual(an_embedded_application.ModuleResourceName);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints.given;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_the_catalog_is_malformed : Specification
{
    WebApplication _app;
    Exception _notJson;
    Exception _withoutDocuments;
    Exception _withoutAResource;
    Exception _withAnUnknownKind;
    Exception _withAnUnknownParent;

    void Establish() => _app = WebApplication.CreateBuilder().Build();

    void Because()
    {
        _notJson = Map("Malformed.NotJson", "this is not json");
        _withoutDocuments = Map("Malformed.WithoutDocuments", """{ "something": [] }""");
        _withoutAResource = Map("Malformed.WithoutAResource", """{ "documents": [ { "id": "A", "kind": "assembly" } ] }""");
        _withAnUnknownKind = Map("Malformed.WithAnUnknownKind", $$"""{ "documents": [ { "id": "A", "kind": "folder", "resourceName": "{{an_embedded_application.ApplicationResourceName}}" } ] }""");
        _withAnUnknownParent = Map("Malformed.WithAnUnknownParent", $$"""{ "documents": [ { "id": "A", "kind": "module", "parentId": "B", "resourceName": "{{an_embedded_application.ApplicationResourceName}}" } ] }""");
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    Exception Map(string name, string catalog)
    {
        try
        {
            _app.MapCratisEventModel(an_embedded_application.WithCatalog(name, catalog));
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null!;
    }

    [Fact] void should_fail_when_the_catalog_is_not_json() => _notJson.ShouldBeOfExactType<MalformedEventModelCatalog>();
    [Fact] void should_fail_when_the_catalog_holds_no_documents_collection() => _withoutDocuments.ShouldBeOfExactType<MalformedEventModelCatalog>();
    [Fact] void should_fail_when_a_document_names_no_resource() => _withoutAResource.ShouldBeOfExactType<MalformedEventModelCatalog>();
    [Fact] void should_fail_when_a_document_declares_an_unknown_kind() => _withAnUnknownKind.ShouldBeOfExactType<MalformedEventModelCatalog>();
    [Fact] void should_fail_when_a_document_names_a_parent_it_does_not_hold() => _withAnUnknownParent.ShouldBeOfExactType<MalformedEventModelCatalog>();
}

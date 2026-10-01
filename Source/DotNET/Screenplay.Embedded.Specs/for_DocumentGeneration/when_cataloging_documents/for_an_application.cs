// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.Generation;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_cataloging_documents;

/// <summary>
/// The catalog is the only thing a reader has to load to build the navigation, and it is written by a build and
/// read by a viewer that never see each other. So its shape - camel cased names, the kind as a word rather than a
/// number, the resource each document really lives under - is specified rather than left to whatever the
/// serializer defaulted to on the day.
/// </summary>
public class for_an_application : Specification
{
    EmbeddedDocumentGeneration _generation;
    string _json;
    JsonElement _documents;

    void Because()
    {
        _generation = new EmbeddedDocumentGenerator()
            .Generate(given.an_application.Build(), given.an_application.Options(), []);
        _json = _generation.Catalog.Serialize();
        _documents = JsonDocument.Parse(_json).RootElement.GetProperty("documents");
    }

    JsonElement DocumentOf(string id) =>
        _documents.EnumerateArray().First(_ => _.GetProperty("id").GetString() == id);

    [Fact] void should_hold_every_generated_document() => _documents.GetArrayLength().ShouldEqual(5);

    [Fact] void should_identify_the_assembly_document_by_the_root_namespace() =>
        DocumentOf(given.an_application.RootNamespace).GetProperty("kind").GetString().ShouldEqual("assembly");

    [Fact] void should_name_the_kind_of_a_module() =>
        DocumentOf(given.an_application.Module).GetProperty("kind").GetString().ShouldEqual("module");

    [Fact] void should_name_the_kind_of_a_feature() =>
        DocumentOf(given.an_application.Feature).GetProperty("kind").GetString().ShouldEqual("feature");

    [Fact] void should_state_the_namespace_exactly_as_the_source_declares_it() =>
        DocumentOf(given.an_application.NestedFeature).GetProperty("namespace").GetString().ShouldEqual(given.an_application.NestedFeature);

    [Fact] void should_state_the_parent_of_a_document() =>
        DocumentOf(given.an_application.Feature).GetProperty("parentId").GetString().ShouldEqual(given.an_application.Module);

    [Fact] void should_leave_the_assembly_document_without_a_parent() =>
        DocumentOf(given.an_application.RootNamespace).GetProperty("parentId").ValueKind.ShouldEqual(JsonValueKind.Null);

    [Fact] void should_name_the_resource_each_document_is_embedded_under() =>
        DocumentOf(given.an_application.Feature).GetProperty("resourceName").GetString()
            .ShouldEqual($"Cratis.Arc.Screenplay.Embedded.documents.{given.an_application.Feature}.play");

    [Fact] void should_title_a_document_after_the_part_of_the_application_it_describes() =>
        DocumentOf(given.an_application.Module).GetProperty("title").GetString().ShouldEqual("Accounting");

    [Fact] void should_read_back_into_the_catalog_it_was_written_from() =>
        EmbeddedDocumentCatalog.Deserialize(_json).Documents.SequenceEqual(_generation.Catalog.Documents).ShouldBeTrue();

    [Fact] void should_answer_with_an_empty_catalog_when_there_is_nothing_to_read() =>
        EmbeddedDocumentCatalog.Deserialize(string.Empty).Documents.ShouldBeEmpty();
}

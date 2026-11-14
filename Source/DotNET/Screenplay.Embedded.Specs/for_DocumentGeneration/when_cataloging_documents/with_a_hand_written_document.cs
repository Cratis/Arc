// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_cataloging_documents;

public class with_a_hand_written_document : Specification
{
    EmbeddedDocument _authored;
    EmbeddedDocumentCatalog _readBack;
    JsonElement _documents;

    void Because()
    {
        var generated = new EmbeddedDocumentGenerator()
            .Generate(given.an_application.Build(), given.an_application.Options(), []);
        _authored = new EmbeddedDocument("Authored", "Authored", "Authored", EmbeddedDocumentKind.Assembly, null, EmbeddedResourceNames.ForDocument("Authored"));
        var json = new EmbeddedDocumentCatalog([.. generated.Catalog.Documents, _authored]).Serialize();
        _readBack = EmbeddedDocumentCatalog.Deserialize(json);
        using var document = JsonDocument.Parse(json);
        _documents = document.RootElement.GetProperty("documents").Clone();
    }

    [Fact] void should_default_the_authored_document_to_embedded() => _authored.Origin.ShouldEqual(EventModelOrigin.Embedded);
    [Fact] void should_not_stamp_the_authored_document_as_generated() => _documents[_documents.GetArrayLength() - 1].TryGetProperty("origin", out _).ShouldBeFalse();
    [Fact] void should_keep_the_authored_origin_when_read_back() => _readBack.Documents[^1].Origin.ShouldEqual(EventModelOrigin.Embedded);
    [Fact] void should_keep_the_csharp_documents_generated() => _readBack.Documents.SkipLast(1).All(_ => _.Origin == EventModelOrigin.Generated).ShouldBeTrue();
}

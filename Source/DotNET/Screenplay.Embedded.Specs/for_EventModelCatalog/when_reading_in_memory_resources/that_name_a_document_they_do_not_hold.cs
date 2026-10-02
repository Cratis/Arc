// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelCatalog.when_reading_in_memory_resources;

/// <summary>
/// Resources held in memory are read by the same rules as an assembly's manifest resources, so a catalog promising
/// a document that is not there fails the same way instead of serving a tree with a hole in it.
/// </summary>
public class that_name_a_document_they_do_not_hold : Specification
{
    const string ProjectName = "Library";

    InMemoryEventModelResources _resources;
    Exception _error;

    void Establish()
    {
        var catalog = new EmbeddedDocumentCatalog(
            [new EmbeddedDocument(ProjectName, ProjectName, ProjectName, EmbeddedDocumentKind.Assembly, null, EmbeddedResourceNames.ForDocument(ProjectName))]);
        _resources = new(ProjectName, new Dictionary<string, string> { [EmbeddedResourceNames.Catalog] = catalog.Serialize() });
    }

    void Because() => _error = Catch.Exception(() => EventModelCatalog.For([_resources]));

    [Fact] void should_fail_as_a_malformed_catalog() => _error.ShouldBeOfExactType<MalformedEventModelCatalog>();

    [Fact] void should_name_the_project() => _error.Message.Contains(ProjectName, StringComparison.Ordinal).ShouldBeTrue();
}

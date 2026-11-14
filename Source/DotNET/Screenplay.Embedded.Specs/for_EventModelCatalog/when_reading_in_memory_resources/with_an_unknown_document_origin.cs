// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelCatalog.when_reading_in_memory_resources;

public class with_an_unknown_document_origin : Specification
{
    Exception _error;

    void Because()
    {
        var resources = new InMemoryEventModelResources("Records", new Dictionary<string, string>
        {
            [EventModelCatalog.ResourceName] =
                """
                {"documents":[{"id":"Entries","kind":"assembly","resourceName":"entries.play","origin":"unknown"}]}
                """,
            ["entries.play"] = "module Records"
        });
        _error = Catch.Exception(() => EventModelCatalog.For([resources]));
    }

    [Fact] void should_refuse_the_catalog() => _error.ShouldBeOfExactType<MalformedEventModelCatalog>();
    [Fact] void should_name_the_document() => _error.Message.Contains("Entries", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_invalid_origin() => _error.Message.Contains("unknown", StringComparison.Ordinal).ShouldBeTrue();
}

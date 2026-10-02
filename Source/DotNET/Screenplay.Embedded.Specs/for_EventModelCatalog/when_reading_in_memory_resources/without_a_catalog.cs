// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelCatalog.when_reading_in_memory_resources;

/// <summary>
/// A set of resources without a catalog contributes nothing, exactly as an assembly built without the package does.
/// </summary>
public class without_a_catalog : Specification
{
    EventModelCatalog _catalog;

    void Because() => _catalog = EventModelCatalog.For([new InMemoryEventModelResources("Library", new Dictionary<string, string>())]);

    [Fact] void should_hold_no_projects() => _catalog.Projects.ShouldBeEmpty();
}

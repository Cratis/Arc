// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_selecting_observation_overloads
{
    [Fact]
    void should_compile_existing_and_context_free_call_shapes()
    {
        // Compile the calls without starting watches: overload selection is the behavior under test.
        var calls = (IMongoCollection<ObservedDocument> collection, FilterDefinition<ObservedDocument> filterDefinition, FindOptions options) =>
        {
            _ = collection.Observe();
            _ = collection.Observe(document => document.Name == "First");
            _ = collection.Observe(document => document.Name == "First", options);
            _ = collection.Observe(filterDefinition);
            _ = collection.Observe(filterDefinition, options);
            _ = collection.Observe(ignoreQueryContext: true);
            _ = collection.Observe(filterDefinition, ignoreQueryContext: true, options: options);
            _ = collection.Observe(filterDefinition: filterDefinition, ignoreQueryContext: true);
            _ = collection.Observe(ignoreQueryContext: true, filter: filterDefinition, options: options);
            _ = collection.Observe(document => document.Name == "First", ignoreQueryContext: true, options: options);

            _ = collection.ObserveSingle();
            _ = collection.ObserveSingle(document => document.Name == "First", options);
            _ = collection.ObserveSingle(filterDefinition);
            _ = collection.ObserveSingle(filterDefinition, options);
            _ = collection.ObserveSingle(ignoreQueryContext: true);
            _ = collection.ObserveSingle(filterDefinition, ignoreQueryContext: true, options: options);
            _ = collection.ObserveSingle(filterDefinition: filterDefinition, ignoreQueryContext: true);
            _ = collection.ObserveSingle(ignoreQueryContext: true, filter: filterDefinition, options: options);
            _ = collection.ObserveSingle(document => document.Name == "First", ignoreQueryContext: true, options: options);

            _ = collection.ObserveById(Guid.NewGuid());
            _ = collection.ObserveById(Guid.NewGuid(), ignoreQueryContext: true);
        };
        calls.ShouldNotBeNull();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver.when_resolving.given;

public class a_resolver : Specification
{
    protected MongoDBReadModelForCommandResolver _resolver;

    void Establish()
    {
        // Resolving looks up the class map for a read model, and mapping one reaches for process-wide state that only
        // AddCratisMongoDB() establishes: the naming policy the member name convention reads, and the Cratis
        // serializers, which cannot be registered once the driver has cached its own for a Guid. An application always
        // has both in place long before a command resolves anything, so the specification puts them there the same way
        // rather than assembling the pieces itself - assembling them is what left this passing only when some other
        // specification had run AddCratisMongoDB() first.
        new ServiceCollection().AddCratisMongoDB();

        _resolver = new([typeof(Customer), typeof(Account), typeof(Preferences)]);
    }

    protected static CommandContext CommandContextWith<TReadModel>(string? resolvedKey, IMongoCollection<TReadModel>? collection = null)
    {
        // The collection is available in the command scope the same way the MongoDB integration registers it.
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IMongoCollection<TReadModel>)).Returns(collection);

        var values = new CommandContextValues();
        if (resolvedKey is not null)
        {
            values.Add(CommandContextKeys.ResolvedKey, resolvedKey);
        }

        return new CommandContext(
            CorrelationId.New(),
            typeof(object),
            new object(),
            [],
            values,
            ServiceProvider: serviceProvider);
    }

    protected static IMongoCollection<TReadModel> CollectionHolding<TReadModel>(params TReadModel[] documents)
    {
        // A find by id hands back the given documents.
        var cursor = Substitute.For<IAsyncCursor<TReadModel>>();
        cursor.Current.Returns(documents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(documents.Length > 0), Task.FromResult(false));

        var collection = Substitute.For<IMongoCollection<TReadModel>>();
        collection
            .FindAsync(Arg.Any<FilterDefinition<TReadModel>>(), Arg.Any<FindOptions<TReadModel, TReadModel>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        return collection;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an <see cref="ICanResolveReadModelForCommand"/> that resolves read models held in MongoDB through the
/// collection for the read model, keyed by the command's resolved key.
/// </summary>
/// <param name="readModelTypes">The read model types this resolver can resolve by key.</param>
/// <remarks>
/// MongoDB holds a document per read model instance, keyed by <c>_id</c>, so the command's resolved key is the document
/// id. Nothing in an application declares that a read model is stored in MongoDB, which is why this claims its types as a
/// <see cref="ReadModelForCommandOwnership.Fallback"/>: a provider that does own a read model — Chronicle for one it
/// projects, Entity Framework Core for one carried by a read model context — keeps it.
/// </remarks>
public class MongoDBReadModelForCommandResolver(IEnumerable<Type> readModelTypes) : ICanResolveReadModelForCommand
{
    static readonly MethodInfo _findByIdAsyncMethod = typeof(MongoCollectionExtensions).GetMethod(
        nameof(MongoCollectionExtensions.FindByIdAsync),
        BindingFlags.Static | BindingFlags.Public)!;

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelTypes { get; } = [.. readModelTypes];

    /// <inheritdoc/>
    public ReadModelForCommandOwnership Ownership => ReadModelForCommandOwnership.Fallback;

    /// <summary>
    /// Discovers the read model types MongoDB can resolve by key from the types of the application.
    /// </summary>
    /// <param name="types">The types to inspect.</param>
    /// <returns>The read model types MongoDB can hold a document per.</returns>
    /// <remarks>
    /// Every <c>[ReadModel]</c> is a candidate, because the MongoDB integration serves a collection for any read model
    /// type without being told about it up front. Claiming them as a fallback is what keeps that breadth from taking a
    /// read model away from the provider that owns it.
    /// </remarks>
    public static IEnumerable<Type> DiscoverReadModelTypes(IEnumerable<Type> types) =>
        types.Where(type => type.IsClass && !type.IsAbstract && type.IsReadModel());

    /// <inheritdoc/>
    /// <exception cref="UnableToResolveReadModelFromCommandContext">Thrown when the command carries no usable key to resolve the read model by; it surfaces as a validation failure (HTTP 400).</exception>
    /// <exception cref="MissingIdMapping">Thrown when the read model has no member mapped to the document id to resolve by.</exception>
    public async Task<object?> Resolve(Type readModelType, CommandContext commandContext)
    {
        var resolvedKey = commandContext.GetResolvedKey();
        if (string.IsNullOrEmpty(resolvedKey))
        {
            // A read model is keyed by the command's resolved key, so an absent key can never resolve one — for a
            // nullable and a non-nullable dependency alike. That is invalid client input, so it surfaces as a
            // validation failure (HTTP 400) rather than null or an unhandled server error. A valid-but-not-found read
            // model still resolves to null below.
            throw new UnableToResolveReadModelFromCommandContext(readModelType);
        }

        var idMemberMap = BsonClassMap.LookupClassMap(readModelType).IdMemberMap ?? throw new MissingIdMapping(readModelType);
        var key = resolvedKey.ConvertTo(idMemberMap.MemberType);
        var collection = commandContext.ServiceProvider!.GetRequiredService(typeof(IMongoCollection<>).MakeGenericType(readModelType));

        // A never-materialized read model resolves to null; command-side code can inject a nullable read model and
        // treat null as "does not exist", while a non-nullable dependency is surfaced as a validation failure (HTTP 400).
        var find = (Task)_findByIdAsyncMethod
            .MakeGenericMethod(readModelType, idMemberMap.MemberType)
            .Invoke(null, [collection, key])!;

        await find;

        return find.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(find);
    }
}

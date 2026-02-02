// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Integration;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Read model for integration test entities with observable queries.
/// </summary>
/// <param name="Id">The entity identifier.</param>
/// <param name="Name">The entity name.</param>
/// <param name="IsActive">Whether the entity is active.</param>
[ReadModel]
public record IntegrationTestReadModel(int Id, string Name, bool IsActive)
{
    /// <summary>
    /// Observe all entities.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <returns>An observable subject of entities.</returns>
    public static ISubject<IEnumerable<IntegrationTestReadModel>> AllEntities(IntegrationTestDbContext dbContext)
    {
        return dbContext.Entities
            .Observe()
            .ToReadModelSubject();
    }

    /// <summary>
    /// Observe active entities only.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <returns>An observable subject of active entities.</returns>
    public static ISubject<IEnumerable<IntegrationTestReadModel>> ActiveEntities(IntegrationTestDbContext dbContext)
    {
        return dbContext.Entities
            .Observe(e => e.IsActive)
            .ToReadModelSubject();
    }
}

/// <summary>
/// Extension methods for converting entity subjects to read model subjects.
/// </summary>
internal static class SubjectExtensions
{
    /// <summary>
    /// Converts an entity subject to a read model subject.
    /// </summary>
    /// <param name="source">The source entity subject.</param>
    /// <returns>A read model subject.</returns>
    internal static ISubject<IEnumerable<IntegrationTestReadModel>> ToReadModelSubject(
        this ISubject<IEnumerable<IntegrationTestEntity>> source)
    {
        var result = new BehaviorSubject<IEnumerable<IntegrationTestReadModel>>([]);
        source.Subscribe(entities =>
        {
            var readModels = entities.Select(e => new IntegrationTestReadModel(e.Id, e.Name, e.IsActive));
            result.OnNext(readModels);
        });
        return result;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkReadModelForCommandResolver;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

[ReadModel]
public class CustomerReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PlainEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// A read-only DbContext carrying a read model entity and a plain entity, used to verify discovery.
/// </summary>
/// <param name="options">The options to be used by the DbContext.</param>
public class CustomerReadModelDbContext(DbContextOptions<CustomerReadModelDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();
    public DbSet<PlainEntity> Plain => Set<PlainEntity>();
}

/// <summary>
/// A writable DbContext over the same read model entity, used to seed data the resolver reads back.
/// </summary>
/// <param name="options">The options to be used by the DbContext.</param>
public class SeedableCustomerDbContext(DbContextOptions<SeedableCustomerDbContext> options) : DbContext(options)
{
    public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();
}

#pragma warning restore SA1402, SA1649

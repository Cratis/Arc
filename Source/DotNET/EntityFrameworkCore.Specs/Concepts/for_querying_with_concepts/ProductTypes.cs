// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

#pragma warning disable SA1402, SA1649

public record ProductId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ProductId NotSet = new(Guid.Empty);
    public static implicit operator ProductId(Guid value) => new(value);
    public static ProductId New() => new(Guid.NewGuid());
}

public record ProductCode(int Value) : ConceptAs<int>(Value)
{
    public static implicit operator ProductCode(int value) => new(value);
}

public record ProductName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator ProductName(string value) => new(value);
}

public class Product
{
    public ProductId Id { get; set; } = ProductId.NotSet;
    public ProductCode Code { get; set; } = null!;
    public ProductName Name { get; set; } = null!;
    public decimal Price { get; set; }
}

public record CategoryId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator CategoryId(Guid value) => new(value);
}

public record CategoryName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator CategoryName(string value) => new(value);
}

public class Category
{
    public CategoryId Id { get; set; } = null!;
    public CategoryName Name { get; set; } = null!;
    public int ItemCount { get; set; }
}

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(
                    v => v.Value,
                    v => new ProductId(v));
                entity.Property(e => e.Code).IsRequired().HasConversion(
                    v => v.Value,
                    v => new ProductCode(v));
                entity.Property(e => e.Name).IsRequired().HasConversion(
                    v => v.Value,
                    v => new ProductName(v));
            })
            .Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(
                    v => v.Value,
                    v => new CategoryId(v));
                entity.Property(e => e.Name).IsRequired().HasConversion(
                    v => v.Value,
                    v => new CategoryName(v));
            });
    }
}

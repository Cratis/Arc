// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A simple read model for testing.
/// </summary>
[ReadModel]
public class SimpleReadModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <returns>Collection of read models.</returns>
    public static IEnumerable<SimpleReadModel> GetAll() =>
    [
        new SimpleReadModel { Id = Guid.NewGuid(), Name = "Item 1", Value = 1 },
        new SimpleReadModel { Id = Guid.NewGuid(), Name = "Item 2", Value = 2 },
        new SimpleReadModel { Id = Guid.NewGuid(), Name = "Item 3", Value = 3 }
    ];

    /// <summary>
    /// Gets a single item by ID.
    /// </summary>
    /// <param name="id">The ID to find.</param>
    /// <returns>The read model or null.</returns>
    public static SimpleReadModel? GetById(Guid id) =>
        new() { Id = id, Name = $"Item {id}", Value = 42 };
}

/// <summary>
/// A read model with query parameters for testing.
/// </summary>
[ReadModel]
public class ParameterizedReadModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Searches by name and category.
    /// </summary>
    /// <param name="name">The name filter.</param>
    /// <param name="category">The category filter.</param>
    /// <returns>Collection of matching read models.</returns>
    public static IEnumerable<ParameterizedReadModel> Search(string name, string category) =>
    [
        new ParameterizedReadModel { Id = Guid.NewGuid(), Name = name, Category = category }
    ];

    /// <summary>
    /// Gets by category with optional limit.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="limit">Optional limit.</param>
    /// <returns>Collection of matching read models.</returns>
    public static IEnumerable<ParameterizedReadModel> GetByCategory(string category, int? limit = null)
    {
        return Enumerable.Range(1, limit ?? 10)
            .Select(i => new ParameterizedReadModel
            {
                Id = Guid.NewGuid(),
                Name = $"Item {i}",
                Category = category
            });
    }

    /// <summary>
    /// Gets items for a specific category filtered by name.
    /// </summary>
    /// <param name="category">The category filter.</param>
    /// <param name="name">The name filter.</param>
    /// <returns>Collection of matching read models.</returns>
    public static IEnumerable<ParameterizedReadModel> GetItemsInCategory(string category, string name) =>
    [
        new ParameterizedReadModel { Id = Guid.NewGuid(), Name = name, Category = category }
    ];
}

/// <summary>
/// A read model requiring authorization for testing.
/// </summary>
[ReadModel]
[Authorize]
public class AuthorizedReadModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the secret data.
    /// </summary>
    public string SecretData { get; set; } = string.Empty;

    /// <summary>
    /// Gets secret data.
    /// </summary>
    /// <returns>The authorized read model.</returns>
    [Authorize]
    public static AuthorizedReadModel GetSecret() =>
        new() { Id = Guid.NewGuid(), SecretData = "Top secret information" };
}

/// <summary>
/// A read model that throws exceptions for testing.
/// </summary>
[ReadModel]
public class ExceptionReadModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets data but throws an exception.
    /// </summary>
    /// <param name="shouldThrow">Whether to throw.</param>
    /// <returns>The read model.</returns>
    /// <exception cref="QueryExecutionFailed">The exception that is thrown when <paramref name="shouldThrow"/> is true.</exception>
    public static ExceptionReadModel GetWithException(bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new QueryExecutionFailed("Intentional query exception");
        }

        return new ExceptionReadModel { Id = Guid.NewGuid() };
    }
}

/// <summary>
/// A complex read model with nested types for testing.
/// </summary>
[ReadModel]
public class ComplexReadModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nested data.
    /// </summary>
    public ReadModelNestedData? NestedData { get; set; }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<ReadModelItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public ReadModelStatus Status { get; set; }

    /// <summary>
    /// Gets a complex item by ID.
    /// </summary>
    /// <param name="id">The ID.</param>
    /// <returns>The complex read model.</returns>
    public static ComplexReadModel GetComplex(Guid id) => new()
    {
        Id = id,
        Name = "Complex Item",
        NestedData = new ReadModelNestedData
        {
            Description = "Nested description",
            Amount = 123.45m,
            CreatedAt = DateTime.UtcNow
        },
        Items =
        [
            new ReadModelItem { ItemId = 1, ItemName = "Sub-item 1" },
            new ReadModelItem { ItemId = 2, ItemName = "Sub-item 2" }
        ],
        Tags = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        },
        Status = ReadModelStatus.Active
    };

    /// <summary>
    /// Gets all complex items.
    /// </summary>
    /// <returns>Collection of complex read models.</returns>
    public static IEnumerable<ComplexReadModel> GetAllComplex() =>
    [
        GetComplex(Guid.NewGuid()),
        GetComplex(Guid.NewGuid())
    ];
}

/// <summary>
/// Nested data for complex read model.
/// </summary>
public class ReadModelNestedData
{
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// An item within a complex read model.
/// </summary>
public class ReadModelItem
{
    /// <summary>
    /// Gets or sets the item ID.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;
}

/// <summary>
/// Status enum for complex read model.
/// </summary>
public enum ReadModelStatus
{
    /// <summary>
    /// Unknown status.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Active status.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Inactive status.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Archived status.
    /// </summary>
    Archived = 3
}

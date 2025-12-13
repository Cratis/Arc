// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ControllerBased;

/// <summary>
/// Controller for testing controller-based queries.
/// </summary>
[ApiController]
[Route("api/controller-queries")]
public class ControllerQueriesController : ControllerBase
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <returns>Collection of items.</returns>
    [HttpGet("items")]
    public ActionResult<IEnumerable<ControllerQueryItem>> GetAll()
    {
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Item 1", Value = 1 },
            new() { Id = Guid.NewGuid(), Name = "Item 2", Value = 2 }
        });
    }

    /// <summary>
    /// Gets a single item by ID.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>The item.</returns>
    [HttpGet("items/{id}")]
    public ActionResult<ControllerQueryItem> GetById([FromRoute] Guid id)
    {
        return Ok(new ControllerQueryItem
        {
            Id = id,
            Name = $"Item {id}",
            Value = 42
        });
    }

    /// <summary>
    /// Searches items with query parameters.
    /// </summary>
    /// <param name="name">The name filter.</param>
    /// <param name="minValue">The minimum value filter.</param>
    /// <param name="maxValue">The maximum value filter.</param>
    /// <returns>Collection of matching items.</returns>
    [HttpGet("items/search")]
    public ActionResult<IEnumerable<ControllerQueryItem>> Search(
        [FromQuery] string? name,
        [FromQuery] int? minValue,
        [FromQuery] int? maxValue)
    {
        return Ok(new List<ControllerQueryItem>
        {
            new() { Id = Guid.NewGuid(), Name = name ?? "Default", Value = minValue ?? 0 }
        });
    }

    /// <summary>
    /// Gets authorized data.
    /// </summary>
    /// <returns>The authorized data.</returns>
    [HttpGet("authorized")]
    [Authorize]
    public ActionResult<ControllerQueryItem> GetAuthorized()
    {
        return Ok(new ControllerQueryItem
        {
            Id = Guid.NewGuid(),
            Name = "Authorized Item",
            Value = 999
        });
    }

    /// <summary>
    /// Gets data that may throw.
    /// </summary>
    /// <param name="shouldThrow">Whether to throw an exception.</param>
    /// <returns>The item.</returns>
    /// <exception cref="QueryExecutionFailed">The exception that is thrown when <paramref name="shouldThrow"/> is true.</exception>
    [HttpGet("exception")]
    public ActionResult<ControllerQueryItem> GetWithException([FromQuery] bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new QueryExecutionFailed("Controller query exception");
        }

        return Ok(new ControllerQueryItem { Id = Guid.NewGuid(), Name = "Success", Value = 1 });
    }

    /// <summary>
    /// Gets complex nested data.
    /// </summary>
    /// <returns>The complex item.</returns>
    [HttpGet("complex")]
    public ActionResult<ControllerComplexItem> GetComplex()
    {
        return Ok(new ControllerComplexItem
        {
            Id = Guid.NewGuid(),
            Name = "Complex Item",
            Nested = new ControllerNestedItem
            {
                Description = "Nested data",
                Amount = 123.45m,
                ProcessingDuration = TimeSpan.FromSeconds(45)
            },
            Items = [1, 2, 3],
            Tags = new Dictionary<string, string>
            {
                ["tag1"] = "value1",
                ["tag2"] = "value2"
            }
        });
    }
}

/// <summary>
/// A query item for controller-based testing.
/// </summary>
public class ControllerQueryItem
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
}

/// <summary>
/// A complex item for controller-based testing.
/// </summary>
public class ControllerComplexItem
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
    /// Gets or sets the nested item.
    /// </summary>
    public ControllerNestedItem? Nested { get; set; }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<int> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];
}

/// <summary>
/// A nested item for controller-based testing.
/// </summary>
public class ControllerNestedItem
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
    /// Gets or sets the processing duration.
    /// </summary>
    public TimeSpan ProcessingDuration { get; set; }
}

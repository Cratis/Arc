// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

/// <summary>
/// Controller for testing controller-based observable queries.
/// </summary>
/// <param name="state">The state for observable controller queries.</param>
[ApiController]
[Route("api/observable-controller-queries")]
public class ObservableControllerQueriesController(ObservableControllerQueriesState state) : ControllerBase
{
    readonly ObservableControllerQueriesState state = state;

    /// <summary>
    /// Gets all items as an observable stream.
    /// </summary>
    /// <returns>Observable collection of items.</returns>
    [HttpGet("observe/items")]
    public ISubject<IEnumerable<ObservableControllerQueryItem>> ObserveAll()
    {
        return state.AllItemsSubject;
    }

    /// <summary>
    /// Gets a single item as an observable stream.
    /// </summary>
    /// <returns>Observable item.</returns>
    [HttpGet("observe/single")]
    public ISubject<ObservableControllerQueryItem> ObserveSingle() => state.SingleItemSubject;

    /// <summary>
    /// Gets a single item by ID as an observable stream.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <returns>Observable item.</returns>
    [HttpGet("observe/items/{id}")]
    public ISubject<ObservableControllerQueryItem> ObserveById([FromRoute] Guid id) =>
        new BehaviorSubject<ObservableControllerQueryItem>(
            new ObservableControllerQueryItem { Id = id, Name = $"Controller Item {id}", Value = 100 });

    /// <summary>
    /// Observes items by category.
    /// </summary>
    /// <param name="category">The category filter.</param>
    /// <returns>Observable collection of items.</returns>
    [HttpGet("observe/items/category/{category}")]
    public ISubject<IEnumerable<ObservableControllerQueryItem>> ObserveByCategory([FromRoute] string category)
    {
        if (state.CategorySubjects.TryGetValue(category, out var subject))
        {
            return subject;
        }

        var newSubject = new BehaviorSubject<IEnumerable<ObservableControllerQueryItem>>([
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = $"{category} Controller Item 1", Value = 1 },
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = $"{category} Controller Item 2", Value = 2 }
        ]);
        state.CategorySubjects[category] = newSubject;
        return newSubject;
    }

    /// <summary>
    /// Observes items with query parameters.
    /// </summary>
    /// <param name="name">The name filter.</param>
    /// <param name="minValue">The minimum value filter.</param>
    /// <returns>Observable collection of items.</returns>
    [HttpGet("observe/items/search")]
    public ISubject<IEnumerable<ObservableControllerQueryItem>> ObserveSearch(
        [FromQuery] string? name,
        [FromQuery] int? minValue) =>
        new BehaviorSubject<IEnumerable<ObservableControllerQueryItem>>([
            new ObservableControllerQueryItem { Id = Guid.NewGuid(), Name = name ?? "Default", Value = minValue ?? 0 }
        ]);

    /// <summary>
    /// Observes complex data.
    /// </summary>
    /// <returns>Observable collection of complex items.</returns>
    [HttpGet("observe/complex")]
    public ISubject<IEnumerable<ComplexObservableControllerItem>> ObserveComplex() =>
        new BehaviorSubject<IEnumerable<ComplexObservableControllerItem>>([
            new ComplexObservableControllerItem
            {
                Id = Guid.NewGuid(),
                Name = "Complex Controller Item 1",
                Metadata = new ControllerMetadata { CreatedAt = DateTime.UtcNow, Tags = ["controller", "tag1"] },
                Items = [new ControllerNestedItem { Key = "key1", Value = "value1" }]
            }
        ]);

    /// <summary>
    /// Updates all items - for testing data changes.
    /// </summary>
    /// <param name="items">The new items.</param>
    /// <returns>Action result.</returns>
    [HttpPost("update/items")]
    public IActionResult UpdateAllItems([FromBody] ObservableControllerQueryItem[] items)
    {
        state.AllItemsSubject.OnNext(items);
        return Ok();
    }

    /// <summary>
    /// Updates the single item - for testing data changes.
    /// </summary>
    /// <param name="item">The new item.</param>
    /// <returns>Action result.</returns>
    [HttpPost("update/single")]
    public IActionResult UpdateSingleItem([FromBody] ObservableControllerQueryItem item)
    {
        state.SingleItemSubject.OnNext(item);
        return Ok();
    }

    /// <summary>
    /// Updates items for a category - for testing data changes.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="items">The new items.</param>
    /// <returns>Action result.</returns>
    [HttpPost("update/category/{category}")]
    public IActionResult UpdateItemsForCategory([FromRoute] string category, [FromBody] ObservableControllerQueryItem[] items)
    {
        if (state.CategorySubjects.TryGetValue(category, out var subject))
        {
            subject.OnNext(items);
        }

        return Ok();
    }
}

/// <summary>
/// A query item for controller-based observable queries.
/// </summary>
public class ObservableControllerQueryItem
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
/// A complex query item for controller-based observable queries.
/// </summary>
public class ComplexObservableControllerItem
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
    /// Gets or sets the metadata.
    /// </summary>
    public ControllerMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public IEnumerable<ControllerNestedItem> Items { get; set; } = [];
}

/// <summary>
/// Metadata for complex controller item.
/// </summary>
public class ControllerMetadata
{
    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];
}

/// <summary>
/// Nested item for complex controller item.
/// </summary>
public class ControllerNestedItem
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

/// <summary>
/// A simple observable read model for testing.
/// </summary>
[ReadModel]
public class ObservableReadModel
{
    static readonly BehaviorSubject<IEnumerable<ObservableReadModel>> _allItemsSubject = new([
        new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Observable Item 1", Value = 1 },
        new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Observable Item 2", Value = 2 },
        new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Observable Item 3", Value = 3 }
    ]);

    static readonly BehaviorSubject<ObservableReadModel> _singleItemSubject = new(
        new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Observable Item", Value = 42 }
    );

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
    /// Gets all items as an observable stream.
    /// </summary>
    /// <returns>Observable collection of read models.</returns>
    public static ISubject<IEnumerable<ObservableReadModel>> ObserveAll()
    {
        var newSubject = new BehaviorSubject<IEnumerable<ObservableReadModel>>(_allItemsSubject.Value);
        _allItemsSubject.Subscribe(newSubject);
        return newSubject;
    }

    /// <summary>
    /// Gets a single item by ID as an observable stream.
    /// </summary>
    /// <param name="id">The ID to find.</param>
    /// <returns>Observable read model.</returns>
    public static ISubject<ObservableReadModel> ObserveById(Guid id) =>
        new BehaviorSubject<ObservableReadModel>(
            new ObservableReadModel { Id = id, Name = $"Observable Item {id}", Value = 100 });

    /// <summary>
    /// Gets a single item as an observable stream.
    /// </summary>
    /// <returns>Observable read model.</returns>
    public static ISubject<ObservableReadModel> ObserveSingle()
    {
        var newSubject = new BehaviorSubject<ObservableReadModel>(_singleItemSubject.Value);
        _singleItemSubject.Subscribe(newSubject);
        return newSubject;
    }

    /// <summary>
    /// Updates all items - for testing data changes.
    /// </summary>
    /// <param name="items">The new items.</param>
    public static void UpdateAllItems(IEnumerable<ObservableReadModel> items) => _allItemsSubject.OnNext(items);

    /// <summary>
    /// Updates the single item - for testing data changes.
    /// </summary>
    /// <param name="item">The new item.</param>
    public static void UpdateSingleItem(ObservableReadModel item) => _singleItemSubject.OnNext(item);

    /// <summary>
    /// Resets all static subjects to their initial values - for test isolation.
    /// </summary>
    public static void Reset()
    {
        _allItemsSubject.OnNext([
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Observable Item 1", Value = 1 },
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Observable Item 2", Value = 2 },
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Observable Item 3", Value = 3 }
        ]);

        _singleItemSubject.OnNext(
            new ObservableReadModel { Id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Observable Item", Value = 42 }
        );
    }
}

/// <summary>
/// An observable read model with parameters for testing.
/// </summary>
[ReadModel]
public class ParameterizedObservableReadModel
{
    static readonly Dictionary<string, BehaviorSubject<IEnumerable<ParameterizedObservableReadModel>>> _subjectsByCategory = [];

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
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Observes items by category.
    /// </summary>
    /// <param name="category">The category filter.</param>
    /// <returns>Observable collection of matching read models.</returns>
    public static ISubject<IEnumerable<ParameterizedObservableReadModel>> ObserveByCategory(string category)
    {
        if (!_subjectsByCategory.ContainsKey(category))
        {
            _subjectsByCategory[category] = new BehaviorSubject<IEnumerable<ParameterizedObservableReadModel>>([
                new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = $"{category} Item 1", Category = category, Value = 1 },
                new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = $"{category} Item 2", Category = category, Value = 2 }
            ]);
        }

        var categorySubject = _subjectsByCategory[category];
        var newSubject = new BehaviorSubject<IEnumerable<ParameterizedObservableReadModel>>(categorySubject.Value);
        categorySubject.Subscribe(newSubject);
        return newSubject;
    }

    /// <summary>
    /// Observes items with name and category filters.
    /// </summary>
    /// <param name="name">The name filter.</param>
    /// <param name="category">The category filter.</param>
    /// <returns>Observable collection of matching read models.</returns>
    public static ISubject<IEnumerable<ParameterizedObservableReadModel>> ObserveByNameAndCategory(string name, string category) =>
        new BehaviorSubject<IEnumerable<ParameterizedObservableReadModel>>([
            new ParameterizedObservableReadModel { Id = Guid.NewGuid(), Name = name, Category = category, Value = 50 }
        ]);

    /// <summary>
    /// Updates items for a category - for testing data changes.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="items">The new items.</param>
    public static void UpdateItemsForCategory(string category, IEnumerable<ParameterizedObservableReadModel> items)
    {
        if (_subjectsByCategory.TryGetValue(category, out var subject))
        {
            subject.OnNext(items);
        }
    }

    /// <summary>
    /// Resets all category subjects - for test isolation.
    /// </summary>
    public static void Reset()
    {
        _subjectsByCategory.Clear();
    }
}

/// <summary>
/// An observable read model for complex data testing.
/// </summary>
[ReadModel]
public class ComplexObservableReadModel
{
    static readonly BehaviorSubject<IEnumerable<ComplexObservableReadModel>> _complexItemsSubject = new([
        new ComplexObservableReadModel
        {
            Id = Guid.NewGuid(),
            Name = "Complex Item 1",
            Metadata = new Metadata { CreatedAt = DateTime.UtcNow, Tags = ["tag1", "tag2"] },
            Items = [new NestedItem { Key = "key1", Value = "value1" }]
        }
    ]);

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
    public Metadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public IEnumerable<NestedItem> Items { get; set; } = [];

    /// <summary>
    /// Observes all complex items.
    /// </summary>
    /// <returns>Observable collection of complex read models.</returns>
    public static ISubject<IEnumerable<ComplexObservableReadModel>> ObserveComplex()
    {
        var newSubject = new BehaviorSubject<IEnumerable<ComplexObservableReadModel>>(_complexItemsSubject.Value);
        _complexItemsSubject.Subscribe(newSubject);
        return newSubject;
    }

    /// <summary>
    /// Updates complex items - for testing data changes.
    /// </summary>
    /// <param name="items">The new items.</param>
    public static void UpdateComplexItems(IEnumerable<ComplexObservableReadModel> items) => _complexItemsSubject.OnNext(items);

    /// <summary>
    /// Resets complex items to initial values - for test isolation.
    /// </summary>
    public static void Reset()
    {
        _complexItemsSubject.OnNext([
            new ComplexObservableReadModel
            {
                Id = Guid.NewGuid(),
                Name = "Complex Item 1",
                Metadata = new Metadata { CreatedAt = DateTime.UtcNow, Tags = ["tag1", "tag2"] },
                Items = [new NestedItem { Key = "key1", Value = "value1" }]
            }
        ]);
    }
}

/// <summary>
/// Metadata for complex read model.
/// </summary>
public class Metadata
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
/// Nested item for complex read model.
/// </summary>
public class NestedItem
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

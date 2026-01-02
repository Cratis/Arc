// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

/// <summary>
/// Service to hold observable query state for testing - registered as singleton.
/// </summary>
public class ObservableControllerQueriesState
{
    public BehaviorSubject<IEnumerable<ObservableControllerQueryItem>> AllItemsSubject { get; } = new([
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Controller Item 1", Value = 1 },
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Controller Item 2", Value = 2 },
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Controller Item 3", Value = 3 }
    ]);

    public BehaviorSubject<ObservableControllerQueryItem> SingleItemSubject { get; } = new(
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Controller Item", Value = 42 }
    );

    public Dictionary<string, BehaviorSubject<IEnumerable<ObservableControllerQueryItem>>> CategorySubjects { get; } = [];

    public void Reset()
    {
        AllItemsSubject.OnNext([
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Controller Item 1", Value = 1 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Controller Item 2", Value = 2 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Controller Item 3", Value = 3 }
        ]);

        SingleItemSubject.OnNext(
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Controller Item", Value = 42 });

        CategorySubjects.Clear();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;

/// <summary>
/// Service to hold observable query state for testing - registered as singleton.
/// </summary>
public class ObservableControllerQueriesState
{
    readonly BehaviorSubject<IEnumerable<ObservableControllerQueryItem>> _allItemsSubject = new([
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Controller Item 1", Value = 1 },
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Controller Item 2", Value = 2 },
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Controller Item 3", Value = 3 }
    ]);

    readonly BehaviorSubject<ObservableControllerQueryItem> _singleItemSubject = new(
        new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Controller Item", Value = 42 }
    );

    readonly Dictionary<string, BehaviorSubject<IEnumerable<ObservableControllerQueryItem>>> _categorySubjects = [];

    public BehaviorSubject<IEnumerable<ObservableControllerQueryItem>> AllItemsSubject => _allItemsSubject;
    public BehaviorSubject<ObservableControllerQueryItem> SingleItemSubject => _singleItemSubject;
    public Dictionary<string, BehaviorSubject<IEnumerable<ObservableControllerQueryItem>>> CategorySubjects => _categorySubjects;

    public void Reset()
    {
        _allItemsSubject.OnNext([
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), Name = "Controller Item 1", Value = 1 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), Name = "Controller Item 2", Value = 2 },
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3), Name = "Controller Item 3", Value = 3 }
        ]);

        _singleItemSubject.OnNext(
            new ObservableControllerQueryItem { Id = new Guid(0x10000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66), Name = "Single Controller Item", Value = 42 }
        );

        _categorySubjects.Clear();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public sealed class ObservableScenarioState
{
    public ISubject<ObservableScenarioReadModel> Subject { get; set; } = new BehaviorSubject<ObservableScenarioReadModel>(new("Initial"));
    public BehaviorSubject<IEnumerable<ObservableScenarioReadModel>> Collection { get; } = new([new("First"), new("Second")]);
    public ObservableScenarioChanges Changes { get; } = new();
    public IAsyncEnumerable<ObservableScenarioReadModel> Enumerable { get; set; } = default!;
    public QueryContext Context { get; set; } = QueryContext.NotSet;
    public ObservableQueryScope Scope { get; set; } = default!;
}

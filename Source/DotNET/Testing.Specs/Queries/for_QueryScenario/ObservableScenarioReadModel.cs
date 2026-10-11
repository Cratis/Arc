// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record ObservableScenarioReadModel(string Name)
{
    [AllowAnonymous]
    public static ISubject<ObservableScenarioReadModel> Current(ObservableScenarioState state, IQueryContextManager contexts)
    {
        state.Context = contexts.Current;
        return state.Subject;
    }

    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservableScenarioReadModel>> Collection(ObservableScenarioState state, IQueryContextManager contexts)
    {
        state.Context = contexts.Current;
        state.Context.TotalItems = 42;
        return state.Collection;
    }

    [AllowAnonymous]
    public static IAsyncEnumerable<ObservableScenarioReadModel> Async(ObservableScenarioState state) => state.Changes.Read();

    [Authorize(Policy = "CanRead")]
    public static ISubject<ObservableScenarioReadModel> Restricted(ObservableScenarioState state) => state.Subject;

    [Authorize(Policy = "CanRead")]
    public static ISubject<ObservableScenarioReadModel> Scoped(ObservableScenarioState state, ObservableQueryScope scope)
    {
        state.Scope = scope;
        return state.Subject;
    }
}

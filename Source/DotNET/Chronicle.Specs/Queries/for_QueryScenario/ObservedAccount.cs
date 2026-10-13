// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

[ReadModel]
[FromEvent<ObservedAccountChanged>]
public record ObservedAccount(string Id, decimal Balance)
{
    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservedAccount>> All(IReadModels readModels) => readModels.Observe<ObservedAccount>();

    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservedAccount>> Positive(IReadModels readModels) => readModels.Observe<ObservedAccount>(account => account.Balance > 0);
}

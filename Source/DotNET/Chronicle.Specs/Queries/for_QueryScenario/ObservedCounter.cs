// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

[ReadModel]
public record ObservedCounter(string Id, decimal Total)
{
    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservedCounter>> All(IReadModels readModels) => readModels.Observe<ObservedCounter>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

public class ObservedCounterProjection : IProjectionFor<ObservedCounter>
{
    public void Define(IProjectionBuilderFor<ObservedCounter> builder) => builder
        .From<ObservedAccountChanged>(from => from.Set(model => model.Total).To(@event => @event.Balance))
        .From<ObservedAmountAdded>(from => from.Add(model => model.Total).With(@event => @event.Amount));
}

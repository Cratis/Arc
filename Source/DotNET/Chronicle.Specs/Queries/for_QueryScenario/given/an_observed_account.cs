// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.given;

public class an_observed_account : Specification
{
    protected readonly QueryScenario<ObservedAccount> _scenario = new();
    protected readonly EventSourceId _id = EventSourceId.New();
    protected QueryObservation _observation = default!;

    void Destroy() => _scenario.Dispose();
}

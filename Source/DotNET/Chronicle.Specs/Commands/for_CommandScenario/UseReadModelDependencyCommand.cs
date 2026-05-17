// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

[Command]
public record UseReadModelDependencyCommand(EventSourceId EventSourceId)
{
    public ReadModelDependencyUsed Handle(AccountBalanceReadModel readModel) =>
        new(readModel.Balance);
}

[ReadModel]
public record AccountBalanceReadModel(decimal Balance);

[EventType("de9f2e68-9a50-4e08-9c0f-3f7a2f6c47a1")]
public record ReadModelDependencyUsed(decimal Balance);

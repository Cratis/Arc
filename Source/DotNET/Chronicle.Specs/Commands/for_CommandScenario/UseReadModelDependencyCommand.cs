// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

public record ReadModelDependencyUsed(decimal Balance);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Chronicle;

[Command]
public record MyCommand(EventSourceId EventSourceId)
{
    public Task Handle(MyReadModel readModel)
    {
        return Task.CompletedTask;
    }
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

public record AttemptNestedCommand : ICommandOperation
{
    public Task Execute(ICommandPipeline commands) => commands.Execute(new NestedOperationChild());

    public void Compensate(INestedOperationProbe probe) => probe.Compensated();
}

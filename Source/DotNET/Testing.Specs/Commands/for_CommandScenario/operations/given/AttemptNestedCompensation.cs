// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

public record AttemptNestedCompensation : ICommandOperation
{
    public void Execute() => throw new InvalidCommandOperation("The forward operation failed before nested compensation.");

    public Task Compensate(ICommandPipeline commands) => commands.Execute(new NestedOperationChild());
}

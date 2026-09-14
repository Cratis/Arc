// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public record ScopedProbeOperation : ICommandOperation
{
    public void Execute(ScopedOperationProbe dependency)
    {
        dependency.Use("execute");
        throw new InvalidCommandOperation("The forward operation failed.");
    }

    public void Compensate(ScopedOperationProbe dependency) => dependency.Use("compensate");
}

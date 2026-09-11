// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

public record RecoverableOperation : ICommandOperation
{
    public void Execute(INestedOperationProbe probe) => probe.Following();

    public void Compensate(INestedOperationProbe probe) => probe.Compensated();
}

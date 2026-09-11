// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations.given;

[Command]
public record AttemptNestedRecoveryBatch
{
    public CommandOperations Handle() => [new RecoverableOperation(), new AttemptNestedCompensation()];
}

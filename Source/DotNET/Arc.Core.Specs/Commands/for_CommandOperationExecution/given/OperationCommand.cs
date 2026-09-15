// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public record OperationCommand(CommandOperations Operations)
{
    public CommandOperations Handle() => Operations;
}
